using UnityEngine;

namespace StreamStar
{
    public class GearDef
    {
        public string Id, Name, Icon;
        public double BaseCost, BaseIncome;
        public GearDef(string id, string name, double cost, double income)
        { Id = id; Name = name; Icon = "gear_" + id; BaseCost = cost; BaseIncome = income; }
    }

    public class SkillDef
    {
        public string Id, Name, Icon;
        public double BaseCost, Growth;
        public int MaxLevel; // 0 = unlimited
        public SkillDef(string id, string name, double cost, double growth, int max)
        { Id = id; Name = name; Icon = "skill_" + id; BaseCost = cost; Growth = growth; MaxLevel = max; }
    }

    public class DecorDef
    {
        public string Id, Name, Icon;
        public double Cost;
        public float Bonus;
        /// <summary>Sprite canvas placed inside the room background, normalised to the image (0,0 = top-left, 1,1 = bottom-right).</summary>
        public Rect RoomRect;
        public int Z; // draw order inside the room, back to front
        public DecorDef(string id, string name, double cost, float bonus, float x0, float y0, float x1, float y1, int z)
        {
            Id = id; Name = name; Icon = "BackgroundRoom/room-assets/" + id; Cost = cost; Bonus = bonus;
            RoomRect = Rect.MinMaxRect(x0, y0, x1, y1); Z = z;
        }
    }

    public class OutfitDef
    {
        /// <summary>Id is the character folder name under Resources/Art/Characters (and the avatar file name under Art/Avatar).</summary>
        public string Id, Name, Folder, Avatar;
        public int GemCost;
        public float TapBonus, IncomeBonus;
        public Color Tint; // used when no art is found
        public OutfitDef(string id, string name, int gems, float tap, float income, Color tint)
        {
            Id = id; Name = name; GemCost = gems; TapBonus = tap; IncomeBonus = income; Tint = tint;
            Folder = "Characters/" + id + "/";
            Avatar = "Avatar/" + id;
        }

        /// <summary>Avatar art when it exists, otherwise the idle pose (list icons and the profile card).</summary>
        public string IconName => ArtLibrary.Get(Avatar) != null ? Avatar : Folder + "Idle_0";
    }

    public class RankDef
    {
        public string Name;
        public double Followers;
        public Color Color;
        public RankDef(string name, double followers, Color color) { Name = name; Followers = followers; Color = color; }
    }

    public static class Palette
    {
        public static readonly Color Purple = Hex("7C4DFF");
        public static readonly Color Pink = Hex("FF5FA2");
        public static readonly Color Cyan = Hex("35D6ED");
        public static readonly Color Yellow = Hex("FFD23F");
        public static readonly Color Mint = Hex("4ADE80");
        public static readonly Color Navy = Hex("1B1436");
        public static readonly Color NavyLight = Hex("2C2256");
        public static readonly Color Row = Hex("372B6B");
        public static readonly Color Grey = Hex("6B668A");
        public static readonly Color Red = Hex("FF3B5C");
        public static readonly Color TextDim = Hex("B9B2E0");

        // light theme: floating HUD cards, bottom sheet, popups
        public static readonly Color Sheet = Hex("EEE5FF");
        public static readonly Color Card = Hex("FFFFFF");
        public static readonly Color Ink = Hex("3B2A6B");
        public static readonly Color InkDim = Hex("8577B0");
        public static readonly Color Track = Hex("D9CCF5");
        public static readonly Color Disabled = Hex("CFC8E2");
        public static readonly Color Green = Hex("16A34A");

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }
    }

    public static class GameConfig
    {
        public const string SaveKey = "streamstar.save.v1";

        /// <summary>Gems are topped up to this amount whenever a save loads (new and existing saves). Set to 0 to disable.</summary>
        public const int MinGemsOnLoad = 90000;

        public const double GearGrowth = 1.15;
        public static readonly int[] GearMilestones = { 10, 25, 50, 100, 150, 200, 250, 300 };

        public static readonly GearDef[] Gear =
        {
            new GearDef("webcam", "Webcam", 15, 0.5),
            new GearDef("mic", "Microphone", 100, 3),
            new GearDef("headset", "Headset", 1100, 20),
            new GearDef("keyboard", "RGB Keyboard", 12000, 120),
            new GearDef("lights", "Ring Lights", 130000, 800),
            new GearDef("chair", "Gaming Chair", 1.4e6, 5000),
            new GearDef("pc", "Gaming PC", 2e7, 40000),
            new GearDef("greenscreen", "Green Screen", 3.3e8, 300000),
        };

        public const int SkillTap = 0, SkillEngage = 1, SkillCrit = 2, SkillHype = 3, SkillOffline = 4;

        public static readonly SkillDef[] Skills =
        {
            new SkillDef("tap", "Tap Power", 10, 1.18, 0),
            new SkillDef("engage", "Engagement", 500, 1.4, 50),
            new SkillDef("crit", "Viral Clip", 250, 1.6, 25),
            new SkillDef("hype", "Hype Train", 1000, 2.0, 10),
            new SkillDef("offline", "Stream Replays", 5000, 2.2, 10),
        };

        public static readonly DecorDef[] Decor =
        {
            new DecorDef("sofa", "Cozy Sofa", 2e3, 0.05f, 0.4825f, 0.4922f, 1.1320f, 0.7087f, 8),
            new DecorDef("sofa-pillow", "Lavender Pillow", 8e3, 0.05f, 0.7177f, 0.4992f, 0.8371f, 0.5538f, 9),
            new DecorDef("teddy-bear", "Teddy Bear", 2.5e4, 0.08f, 0.7452f, 0.4901f, 0.9172f, 0.5698f, 10),
            new DecorDef("wall-art", "City Night Art", 8e4, 0.1f, 0.4175f, 0.2474f, 0.7075f, 0.3441f, 1),
            new DecorDef("bookshelf", "Book Shelf", 3e5, 0.1f, 0.1030f, 0.2217f, 0.5461f, 0.3694f, 2),
            new DecorDef("study-desk", "Study Desk", 1e6, 0.12f, -0.0548f, 0.7206f, 0.5512f, 0.9226f, 11),
            new DecorDef("ceiling-light", "Ceiling Light", 4e6, 0.12f, 0.3339f, 0.0618f, 0.6356f, 0.1373f, 0),
            new DecorDef("livestream-desk", "Stream Desk", 1.5e7, 0.15f, 0.1717f, 0.3555f, 0.6844f, 0.5898f, 4),
            new DecorDef("monitor", "Gaming Monitor", 6e7, 0.15f, 0.3427f, 0.3804f, 0.5863f, 0.4616f, 6),
            new DecorDef("pc-case", "RGB Gaming PC", 2.5e8, 0.2f, 0.4324f, 0.4866f, 0.6704f, 0.5997f, 5),
            new DecorDef("gaming-chair", "Gaming Chair", 1e9, 0.2f, 0.1633f, 0.4196f, 0.4324f, 0.6215f, 7),
            new DecorDef("figure-cabinet", "Figure Cabinet", 5e9, 0.25f, -0.1171f, 0.3008f, 0.2900f, 0.6061f, 3),
        };

        public static readonly OutfitDef[] Outfits =
        {
            new OutfitDef("Character_YellowHoodie", "Cozy Hoodie", 0, 0f, 0f, Palette.Purple),
            new OutfitDef("Striped_TShirt_Denim_Short_Overalls", "Overalls Tee", 10, 0.1f, 0f, Palette.Mint),
            new OutfitDef("Cream_Sweater_Denim_Shorts", "Cozy Sweater", 15, 0f, 0.1f, Palette.Yellow),
            new OutfitDef("White_Tank_Blue_Sport_Shorts", "Sporty Tank", 20, 0.1f, 0f, Palette.Cyan),
            new OutfitDef("Black_Camisole_Teal_Sport_Shorts", "Teal Sport", 25, 0f, 0.1f, Palette.Mint),
            new OutfitDef("Cream_Cardigan_Pink_Slip", "Pink Cardigan", 30, 0.1f, 0f, Palette.Pink),
            new OutfitDef("Cream_Cardigan_Blue_Plaid_Skirt", "Plaid Cardigan", 35, 0f, 0.1f, Palette.Cyan),
            new OutfitDef("Cream_Sweater_Floral_Skirt", "Floral Skirt", 40, 0.1f, 0f, Palette.Pink),
            new OutfitDef("White_Blue_Floral_Sundress", "Blue Sundress", 60, 0f, 0.2f, Palette.Cyan),
            new OutfitDef("White_Tank_Blue_Flared_Skirt", "Flared Skirt", 90, 0f, 0.2f, Palette.Cyan),
            new OutfitDef("White_Halter_Blue_WideLeg_Pants", "Halter Chic", 105, 0.2f, 0f, Palette.Cyan),
            new OutfitDef("White_Blouse_Office_Skirt", "Office Lady", 135, 0.2f, 0f, Palette.Purple),
            new OutfitDef("Navy_White_Maid_Dress", "Navy Maid", 150, 0f, 0.2f, Palette.Purple),
            new OutfitDef("White_Yellow_Sailor_Dress", "Sailor Dress", 165, 0.2f, 0f, Palette.Yellow),
            new OutfitDef("Purple_Cream_Swimsuit", "Summer Swim", 180, 0f, 0.2f, Palette.Pink),
            new OutfitDef("Burgundy_Christmas_Sweater_Dress", "Christmas Sweater", 220, 0.3f, 0f, Palette.Red),
            new OutfitDef("Blue_Short_Qipao", "Blue Qipao", 260, 0f, 0.3f, Palette.Cyan),
            new OutfitDef("White_Blue_Long_Qipao", "Long Qipao", 300, 0.3f, 0f, Palette.Cyan),
            new OutfitDef("Purple_Patterned_Kimono", "Kimono", 340, 0f, 0.3f, Palette.Purple),
            new OutfitDef("White_Lace_Cat_Gown", "Lace Cat Gown", 380, 0.3f, 0f, Palette.Pink),
            new OutfitDef("Burgundy_Business_Suit", "Business Suit", 420, 0f, 0.3f, Palette.Red),
            new OutfitDef("Charcoal_Pinstripe_Suit", "Pinstripe Suit", 460, 0.3f, 0f, Palette.Purple),
            new OutfitDef("Blue_Crystal_Floral_Gown", "Crystal Gown", 600, 0f, 0.5f, Palette.Cyan),
            new OutfitDef("White_Gold_Halter_Gown", "Gold Halter Gown", 750, 0.5f, 0f, Palette.Yellow),
            new OutfitDef("Blue_Gold_Halter_Gown", "Royal Blue Gown", 900, 0f, 0.5f, Palette.Cyan),
            new OutfitDef("Burgundy_SciFi_Armor_Suit", "Sci-Fi Armor", 1050, 0.5f, 0f, Palette.Red),
            new OutfitDef("Royal_Officer_Uniform", "Royal Officer", 1200, 0f, 0.5f, Palette.Yellow),
            // appended after the original set so existing saves (owned/equipped by index) stay valid
            new OutfitDef("Plaid_Shirt_Cafe_Apron", "Cafe Apron", 1350, 0.5f, 0f, Palette.Mint),
            new OutfitDef("Gray_Turtleneck_Sweater_Dress", "Turtleneck Dress", 1650, 0.5f, 0f, Palette.Purple),
            new OutfitDef("Beige_Bomber_Purple_Sweater_Brown_Skirt", "Bomber Jacket", 1800, 0f, 0.5f, Palette.Purple),
            new OutfitDef("Pale_Blue_Sundress_Bunny_Straw_Hat", "Bunny Sundress", 1950, 0.5f, 0f, Palette.Cyan),
            new OutfitDef("White_Blouse_Leather_Vest_Black_Pants", "Leather Vest", 2250, 0.5f, 0f, Palette.Grey),
            new OutfitDef("Purple_OffShoulder_Black_Leather_Skirt", "Off-Shoulder Top", 2400, 0f, 0.6f, Palette.Purple),
            new OutfitDef("Navy_Cream_Ruffled_Swimwear", "Ruffle Swimwear", 2550, 0.6f, 0f, Palette.Cyan),
            new OutfitDef("Purple_White_Maid_Dress", "Violet Maid", 2700, 0f, 0.6f, Palette.Purple),
            new OutfitDef("Pink_Satin_Cat_Pajamas", "Cat Pajamas", 2850, 0.6f, 0f, Palette.Pink),
            new OutfitDef("Burgundy_Lace_Sleepwear", "Lace Sleepwear", 3000, 0f, 0.6f, Palette.Red),
            new OutfitDef("Black_Slip_Blue_Robe", "Silk Robe", 3150, 0.6f, 0f, Palette.Navy),
            new OutfitDef("Black_Lace_Cat_Outfit", "Black Cat", 3300, 0f, 0.8f, Palette.Grey),
            new OutfitDef("Red_Koi_Yukata", "Koi Yukata", 3450, 0.8f, 0f, Palette.Red),
            new OutfitDef("Burgundy_Black_Gothic_Dress", "Gothic Dress", 3600, 0f, 0.8f, Palette.Red),
            new OutfitDef("Black_Gold_Victorian_Gown", "Victorian Gown", 3750, 0.8f, 0f, Palette.Yellow),
            new OutfitDef("White_Blue_Princess_Mage_Dress", "Princess Mage", 3900, 0f, 0.8f, Palette.Cyan),
            new OutfitDef("yellow-bomber-streetwear", "Yellow Bomber", 4200, 0f, 0.8f, Palette.Yellow),
            new OutfitDef("pop-rock-idol", "Pop Rock Idol", 4350, 0.8f, 0f, Palette.Pink),
            new OutfitDef("hiphop-dance", "Hip-Hop Dance", 4500, 0f, 1.0f, Palette.Purple),
            new OutfitDef("penguin-kigurumi", "Penguin Kigurumi", 4650, 1.0f, 0f, Palette.Cyan),
            new OutfitDef("reindeer-christmas", "Reindeer Christmas", 4800, 0f, 1.0f, Palette.Red),
        };

        public static readonly RankDef[] Ranks =
        {
            new RankDef("Newcomer", 0, Palette.Hex("9AA0B5")),
            new RankDef("Rookie", 100, Palette.Hex("6FCF97")),
            new RankDef("Rising Star", 1e3, Palette.Hex("35D6ED")),
            new RankDef("Affiliate", 1e4, Palette.Hex("5B8DEF")),
            new RankDef("Partner", 1e5, Palette.Hex("7C4DFF")),
            new RankDef("Influencer", 1e6, Palette.Hex("C750E8")),
            new RankDef("Celebrity", 1e7, Palette.Hex("FF5FA2")),
            new RankDef("Superstar", 1e8, Palette.Hex("FF8A3D")),
            new RankDef("Legend", 1e9, Palette.Hex("FFD23F")),
        };

        public const float RankMultStep = 0.5f;
        public const int RankGemStep = 5;

        public const float CritMult = 5f;
        public const float HypePerTap = 0.025f;
        public const float HypeDecayDelay = 1.5f;
        public const float HypeDecayPerSec = 0.06f;
        public const float HypeMult = 3f;
        public const float HypeBaseDuration = 8f;

        public const float BoostMult = 2f;
        public const float BoostDuration = 60f;
        public const float BoostCooldown = 300f;

        public const float GiftMinDelay = 25f, GiftMaxDelay = 45f, GiftLifetime = 7f;
        public const float GiftIncomeSeconds = 60f;
        public const float GiftGemChance = 0.2f;

        // donation jar: fills on top of normal income, the player taps to claim it
        public const float DonationRate = 0.5f;
        public const float DonationCapSeconds = 240f;

        public const float OfflineRate = 0.5f;
        public const float OfflineBaseHours = 2f;
        public const float OfflineMinSeconds = 60f;
        public const float AutosaveInterval = 10f;
    }
}
