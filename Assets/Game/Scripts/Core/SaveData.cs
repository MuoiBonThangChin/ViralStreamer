using System;

namespace StreamStar
{
    [Serializable]
    public class SaveData
    {
        public double Coins;
        public double LifetimeCoins;
        public double Followers;
        public int Gems;
        public int[] GearLevels = new int[0];
        public int[] SkillLevels = new int[0];
        public bool[] DecorOwned = new bool[0];
        public bool[] OutfitOwned = new bool[0];
        public int EquippedOutfit;
        public int RankIndex;
        public float BoostRemaining;
        public float BoostCooldown;
        public long LastSaveUnix;
        public long TotalTaps;
        public double DonationPot;

        /// <summary>Makes sure arrays match the current config (new content added after a save was written).</summary>
        public void Normalize()
        {
            GearLevels = Resize(GearLevels, GameConfig.Gear.Length);
            SkillLevels = Resize(SkillLevels, GameConfig.Skills.Length);
            DecorOwned = Resize(DecorOwned, GameConfig.Decor.Length);
            OutfitOwned = Resize(OutfitOwned, GameConfig.Outfits.Length);
            OutfitOwned[0] = true;
            if (EquippedOutfit < 0 || EquippedOutfit >= OutfitOwned.Length || !OutfitOwned[EquippedOutfit]) EquippedOutfit = 0;
            Gems = Math.Max(Gems, GameConfig.MinGemsOnLoad);
            RankIndex = Math.Max(0, Math.Min(RankIndex, GameConfig.Ranks.Length - 1));
            if (double.IsNaN(Coins) || double.IsInfinity(Coins) || Coins < 0) Coins = 0;
            if (double.IsNaN(DonationPot) || double.IsInfinity(DonationPot) || DonationPot < 0) DonationPot = 0;
            if (double.IsNaN(Followers)|| double.IsInfinity(Followers) || Followers < 0) Followers = 0;
        }

        static T[] Resize<T>(T[] source, int length)
        {
            if (source != null && source.Length == length) return source;
            var result = new T[length];
            if (source != null) Array.Copy(source, result, Math.Min(source.Length, length));
            return result;
        }
    }
}
