using System;
using UnityEngine;

namespace StreamStar
{
    /// <summary>All economy rules. Plain C#: the UI reads from it and calls the action methods.</summary>
    public class GameState
    {
        public SaveData Data { get; private set; }

        public event Action<int, int> OnRankUp;          // new rank index, gems rewarded
        public event Action OnHypeStart;
        public event Action OnHypeEnd;
        public event Action OnChanged;                   // purchases / equips: structural change
        public event Action<int> OnGemsGained;

        /// <summary>1, 10 or -1 (max).</summary>
        public int BuyMode = 1;

        public float Hype { get; private set; }          // 0..1
        public bool HypeActive { get; private set; }
        public float HypeTimeLeft { get; private set; }
        public float HypeDuration => GameConfig.HypeBaseDuration + Data.SkillLevels[GameConfig.SkillHype];

        float _sinceLastTap = 999f;
        readonly System.Random _rng = new System.Random();

        public GameState(SaveData data)
        {
            Data = data ?? new SaveData();
            Data.Normalize();
        }

        // ---------------------------------------------------------------- persistence

        public static GameState Load()
        {
            SaveData data = null;
            string json = PlayerPrefs.GetString(GameConfig.SaveKey, "");
            if (!string.IsNullOrEmpty(json))
            {
                try { data = JsonUtility.FromJson<SaveData>(json); }
                catch (Exception e) { Debug.LogWarning("Save data unreadable, starting fresh: " + e.Message); }
            }
            return new GameState(data);
        }

        public void Save()
        {
            Data.LastSaveUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            PlayerPrefs.SetString(GameConfig.SaveKey, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }

        public static void WipeSave() => PlayerPrefs.DeleteKey(GameConfig.SaveKey);

        /// <summary>Returns coins earned while away (already added) and the seconds counted.</summary>
        public double ApplyOfflineEarnings(out double seconds)
        {
            seconds = 0;
            if (Data.LastSaveUnix <= 0) return 0;
            double away = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - Data.LastSaveUnix;
            if (away < GameConfig.OfflineMinSeconds) return 0;

            // timers keep running while away
            Data.BoostCooldown = Mathf.Max(0f, Data.BoostCooldown - (float)Math.Min(away, 1e6));
            Data.BoostRemaining = 0f;

            seconds = Math.Min(away, OfflineCapSeconds);
            double earned = IncomePerSecBase * seconds * GameConfig.OfflineRate;
            if (earned <= 0) return 0;
            AddCoins(earned);
            Data.Followers += Viewers * 0.04 * seconds * GameConfig.OfflineRate;
            CheckRank();
            return earned;
        }

        public double OfflineCapSeconds => (GameConfig.OfflineBaseHours + Data.SkillLevels[GameConfig.SkillOffline]) * 3600.0;

        // ---------------------------------------------------------------- derived values

        public static int MilestonesReached(int level)
        {
            int count = 0;
            for (int i = 0; i < GameConfig.GearMilestones.Length; i++)
                if (level >= GameConfig.GearMilestones[i]) count++;
            return count;
        }

        public static int NextMilestone(int level)
        {
            for (int i = 0; i < GameConfig.GearMilestones.Length; i++)
                if (level < GameConfig.GearMilestones[i]) return GameConfig.GearMilestones[i];
            return -1;
        }

        public double GearIncome(int index) => GearIncomeAt(index, Data.GearLevels[index]);

        public static double GearIncomeAt(int index, int level)
        {
            if (level <= 0) return 0;
            return GameConfig.Gear[index].BaseIncome * level * Math.Pow(2, MilestonesReached(level));
        }

        public bool GearUnlocked(int index) => index == 0 || Data.GearLevels[index - 1] > 0;

        public float DecorBonus
        {
            get
            {
                float sum = 0f;
                for (int i = 0; i < GameConfig.Decor.Length; i++)
                    if (Data.DecorOwned[i]) sum += GameConfig.Decor[i].Bonus;
                return sum;
            }
        }

        public float OutfitIncomeMult
        {
            get
            {
                float sum = 1f;
                for (int i = 0; i < GameConfig.Outfits.Length; i++)
                    if (Data.OutfitOwned[i]) sum += GameConfig.Outfits[i].IncomeBonus;
                return sum;
            }
        }

        public float OutfitTapMult
        {
            get
            {
                float sum = 1f;
                for (int i = 0; i < GameConfig.Outfits.Length; i++)
                    if (Data.OutfitOwned[i]) sum += GameConfig.Outfits[i].TapBonus;
                return sum;
            }
        }

        public float RankMult => 1f + GameConfig.RankMultStep * Data.RankIndex;
        public bool BoostActive => Data.BoostRemaining > 0f;
        public float CurrentHypeMult => HypeActive ? GameConfig.HypeMult : 1f;

        /// <summary>Income without temporary multipliers (boost, hype).</summary>
        public double IncomePerSecBase
        {
            get
            {
                double sum = 0;
                for (int i = 0; i < GameConfig.Gear.Length; i++) sum += GearIncome(i);
                return sum * (1.0 + DecorBonus) * OutfitIncomeMult * RankMult;
            }
        }

        public double IncomePerSec => IncomePerSecBase * (BoostActive ? GameConfig.BoostMult : 1f) * CurrentHypeMult;

        public double TapBase
        {
            get
            {
                int level = Data.SkillLevels[GameConfig.SkillTap];
                return (1 + level) * Math.Pow(2, level / 10);
            }
        }

        public float EngagePercent => 0.01f * Data.SkillLevels[GameConfig.SkillEngage];
        public float CritChance => 0.02f * Data.SkillLevels[GameConfig.SkillCrit];

        /// <summary>Tap value before the crit roll.</summary>
        public double TapValue
        {
            get
            {
                double boosted = IncomePerSecBase * (BoostActive ? GameConfig.BoostMult : 1f);
                return (TapBase * RankMult + boosted * EngagePercent) * OutfitTapMult * CurrentHypeMult;
            }
        }

        public double Viewers
        {
            get
            {
                double v = Math.Floor(3 + 4 * Math.Pow(Math.Max(0, IncomePerSecBase), 0.55));
                return HypeActive ? Math.Floor(v * 1.5) : v;
            }
        }

        public bool IsMaxRank => Data.RankIndex >= GameConfig.Ranks.Length - 1;

        public float RankProgress
        {
            get
            {
                if (IsMaxRank) return 1f;
                double from = GameConfig.Ranks[Data.RankIndex].Followers;
                double to = GameConfig.Ranks[Data.RankIndex + 1].Followers;
                return Mathf.Clamp01((float)((Data.Followers - from) / (to - from)));
            }
        }

        // ---------------------------------------------------------------- simulation

        public void Tick(float dt)
        {
            if (dt <= 0f) return;
            if (dt > 5f) dt = 5f; // long hitches are covered by offline earnings instead

            AddCoins(IncomePerSec * dt);
            Data.Followers += Viewers * 0.04 * dt;

            double baseIncome = IncomePerSecBase;
            if (baseIncome > 0)
                Data.DonationPot = Math.Min(DonationCap, Data.DonationPot + baseIncome * GameConfig.DonationRate * dt);

            if (Data.BoostRemaining > 0f) Data.BoostRemaining = Mathf.Max(0f, Data.BoostRemaining - dt);
            else if (Data.BoostCooldown > 0f) Data.BoostCooldown = Mathf.Max(0f, Data.BoostCooldown - dt);

            _sinceLastTap += dt;
            if (HypeActive)
            {
                HypeTimeLeft -= dt;
                Hype = Mathf.Clamp01(HypeTimeLeft / HypeDuration);
                if (HypeTimeLeft <= 0f)
                {
                    HypeActive = false;
                    Hype = 0f;
                    OnHypeEnd?.Invoke();
                }
            }
            else if (_sinceLastTap > GameConfig.HypeDecayDelay && Hype > 0f)
            {
                Hype = Mathf.Max(0f, Hype - GameConfig.HypeDecayPerSec * dt);
            }

            CheckRank();
        }

        /// <summary>Player tapped the streamer. Returns the coins earned.</summary>
        public double Tap(out bool crit)
        {
            crit = _rng.NextDouble() < CritChance;
            double value = TapValue * (crit ? GameConfig.CritMult : 1f);
            AddCoins(value);
            Data.Followers += Data.RankIndex + 1;
            Data.TotalTaps++;
            _sinceLastTap = 0f;

            if (!HypeActive)
            {
                Hype = Mathf.Clamp01(Hype + GameConfig.HypePerTap);
                if (Hype >= 1f)
                {
                    HypeActive = true;
                    HypeTimeLeft = HypeDuration;
                    OnHypeStart?.Invoke();
                }
            }
            CheckRank();
            return value;
        }

        void AddCoins(double amount)
        {
            if (amount <= 0 || double.IsNaN(amount)) return;
            Data.Coins += amount;
            Data.LifetimeCoins += amount;
        }

        void AddGems(int amount)
        {
            if (amount <= 0) return;
            Data.Gems += amount;
            OnGemsGained?.Invoke(amount);
        }

        void CheckRank()
        {
            while (!IsMaxRank && Data.Followers >= GameConfig.Ranks[Data.RankIndex + 1].Followers)
            {
                Data.RankIndex++;
                int gems = GameConfig.RankGemStep * Data.RankIndex;
                AddGems(gems);
                OnRankUp?.Invoke(Data.RankIndex, gems);
            }
        }

        // ---------------------------------------------------------------- purchases

        /// <summary>Cost of buying <paramref name="count"/> levels starting at <paramref name="level"/>.</summary>
        public static double BulkCost(double baseCost, double growth, int level, int count)
        {
            if (count <= 0) return 0;
            return baseCost * Math.Pow(growth, level) * (Math.Pow(growth, count) - 1) / (growth - 1);
        }

        public static int MaxAffordable(double baseCost, double growth, int level, double coins)
        {
            double first = baseCost * Math.Pow(growth, level);
            if (coins < first) return 0;
            int n = (int)Math.Floor(Math.Log(coins * (growth - 1) / first + 1) / Math.Log(growth));
            // guard against floating point error at the boundary
            while (n > 0 && BulkCost(baseCost, growth, level, n) > coins) n--;
            return Math.Max(0, n);
        }

        /// <summary>How many levels the current buy mode would purchase (at least 1, for price display).</summary>
        int PlannedCount(double baseCost, double growth, int level, int maxLevel)
        {
            int count = BuyMode > 0 ? BuyMode : Math.Max(1, MaxAffordable(baseCost, growth, level, Data.Coins));
            if (maxLevel > 0) count = Math.Min(count, maxLevel - level);
            return count;
        }

        public int GearPlannedCount(int i) => PlannedCount(GameConfig.Gear[i].BaseCost, GameConfig.GearGrowth, Data.GearLevels[i], 0);
        public double GearPlannedCost(int i) => BulkCost(GameConfig.Gear[i].BaseCost, GameConfig.GearGrowth, Data.GearLevels[i], GearPlannedCount(i));

        public bool BuyGear(int i)
        {
            if (!GearUnlocked(i)) return false;
            int count = GearPlannedCount(i);
            double cost = GearPlannedCost(i);
            if (count <= 0 || Data.Coins < cost) return false;

            int before = MilestonesReached(Data.GearLevels[i]);
            Data.Coins -= cost;
            Data.GearLevels[i] += count;
            AddGems(MilestonesReached(Data.GearLevels[i]) - before);
            OnChanged?.Invoke();
            return true;
        }

        public bool SkillMaxed(int i) => GameConfig.Skills[i].MaxLevel > 0 && Data.SkillLevels[i] >= GameConfig.Skills[i].MaxLevel;
        public int SkillPlannedCount(int i) => PlannedCount(GameConfig.Skills[i].BaseCost, GameConfig.Skills[i].Growth, Data.SkillLevels[i], GameConfig.Skills[i].MaxLevel);
        public double SkillPlannedCost(int i) => BulkCost(GameConfig.Skills[i].BaseCost, GameConfig.Skills[i].Growth, Data.SkillLevels[i], SkillPlannedCount(i));

        public bool BuySkill(int i)
        {
            if (SkillMaxed(i)) return false;
            int count = SkillPlannedCount(i);
            double cost = SkillPlannedCost(i);
            if (count <= 0 || Data.Coins < cost) return false;
            Data.Coins -= cost;
            Data.SkillLevels[i] += count;
            OnChanged?.Invoke();
            return true;
        }

        public bool BuyDecor(int i)
        {
            if (Data.DecorOwned[i] || Data.Coins < GameConfig.Decor[i].Cost) return false;
            Data.Coins -= GameConfig.Decor[i].Cost;
            Data.DecorOwned[i] = true;
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>Buys the outfit if needed, then equips it.</summary>
        public bool BuyOrEquipOutfit(int i)
        {
            if (!Data.OutfitOwned[i])
            {
                if (Data.Gems < GameConfig.Outfits[i].GemCost) return false;
                Data.Gems -= GameConfig.Outfits[i].GemCost;
                Data.OutfitOwned[i] = true;
            }
            Data.EquippedOutfit = i;
            OnChanged?.Invoke();
            return true;
        }

        public bool ActivateBoost()
        {
            if (BoostActive || Data.BoostCooldown > 0f) return false;
            Data.BoostRemaining = GameConfig.BoostDuration;
            Data.BoostCooldown = GameConfig.BoostCooldown;
            return true;
        }

        public double DonationCap => IncomePerSecBase * GameConfig.DonationRate * GameConfig.DonationCapSeconds;
        public bool DonationFull => Data.DonationPot > 0 && Data.DonationPot >= DonationCap * 0.999;

        /// <summary>Empties the donation jar into the wallet. Returns the coins claimed.</summary>
        public double ClaimDonations()
        {
            double coins = Data.DonationPot;
            Data.DonationPot = 0;
            AddCoins(coins);
            return coins;
        }

        public double CollectGift(out bool gem)
        {
            double coins = Math.Max(50, IncomePerSec * GameConfig.GiftIncomeSeconds);
            AddCoins(coins);
            gem = _rng.NextDouble() < GameConfig.GiftGemChance;
            if (gem) AddGems(1);
            return coins;
        }
    }
}
