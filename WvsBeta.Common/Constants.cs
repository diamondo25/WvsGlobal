using System;
using System.Collections.Generic;

namespace WvsBeta.Common
{
    public static class Constants
    {
        public const ushort MAPLE_VERSION = 7;
        public const ushort MAPLE_CRYPTO_VERSION = 12; // for IV stuff
        public const string MAPLE_PATCH_LOCATION = "";
        public const byte MAPLE_LOCALE = 8;

        // 2011000
        public static int[] EXP = new int[] {
            15, 34, 57, 92, 135, 372, 560, 840, 1242, 1716,
            2360, 3216, 4200, 5460, 7050, 8840, 11040, 13716, 16680, 20216,
            24402, 28980, 34320, 40512, 47216, 54900, 63666, 73080, 83720, 95700,
            108480, 122760, 138666, 155540, 174216, 194832, 216600, 240500, 266682, 294216,
            324240, 356916, 391160, 428280, 468450, 510420, 555680, 604416, 655200, 709716,
            748608, 789631, 832902, 878545, 926689, 977471, 1031036, 1087536, 1147132, 1209994,
            1276301, 1346242, 1420016, 1497832, 1579913, 1666492, 1757815, 1854143, 1955750, 2062925,
            2175973, 2295216, 2420993, 2553663, 2693603, 2841212, 2996910, 3161140, 3334370, 3517093,
            3709829, 3913127, 4127566, 4353756, 4592341, 4844001, 5109452, 5389449, 5684790, 5996316,
            6324914, 6671519, 7037118, 7422752, 7829518, 8258575, 8711144, 9188514, 9692044, 10223168,
            10783397, 11374327, 11997640, 12655110, 13348610, 14080113, 14851703, 15665576, 16524049, 17429566,
            18384706, 19392187, 20454878, 21575805, 22758159, 24005306, 25320796, 26708375, 28171993, 29715818,
            31344244, 33061908, 34873700, 36784778, 38800583, 40926854, 43169645, 45535341, 48030677, 50662758,
            53439077, 56367538, 59456479, 62714694, 66151459, 69776558, 73600313, 77633610, 81887931, 86375389,
            91108760, 96101520, 101367883, 106992842, 112782213, 118962678, 125481832, 132358236, 139611467, 147262175,
            155332142, 163844343, 172823012, 182293713, 192283408, 202820538, 213935103, 225658746, 238024845, 251068606,
            264827165, 279339639, 294647508, 310794191, 327825712, 345790561, 364739883, 384727628, 405810702, 428049128,
            451506220, 476248760, 502347192, 529875818, 558913012, 589541445, 621848316, 655925603, 691870326, 729784819,
            769777027, 811960808, 856456260, 903390063, 952895838, 1005114529, 1060194805, 1118293480, 1179575962, 1244216724,
            1312399800, 1384319309, 1460180007, 1540197871, 1624600714, 1713628833, 1807535693, 1906558648, 2011069705
        };

        public const int PartyMinLevelOffset = 5;
        public const double PartyPerUserBonus = 0.05;
        public const double PartyTotalBonus = 1.0;
        public const int MaxPartyMembers = 6;

        public const byte PlayerLevels = 200;
        public const byte PetLevels = 30;
        public const byte MaxPetName = 12;
        public const byte MinPetName = 4;
        public const byte MaxCharacterName = 12;
        public const byte MinCharacterName = 4;
        public const byte MaxSpeakerTextLength = 60;
        public const short MaxMaxHp = 30000;
        public const short MinMaxHp = 1;
        public const short MaxMaxMp = 30000;
        public const short MinMaxMp = 1;
        public const short MaxStat = 2000;
        public const short MinStat = 0;
        public const short MaxFame = 30000;
        public const short MinFame = -30000;
        public const short MaxCloseness = 30000;
        public const short ApPerLevel = 5;
        public const short SpPerLevel = 3;
        public const byte MaxFullness = 100;
        public const byte MinFullness = 0;
        public const byte PetFeedFullness = 30;
        public const int MaxDamage = 99999;
        public const int InvalidMap = 999999999;

        public static short[] PetExp = new short[PetLevels - 1] {
            1, 3, 6, 14, 31, 60, 108, 181, 287, 434,
            632, 891, 1224, 1642, 2161, 2793, 3557, 4467, 5542, 6801,
            8263, 9950, 11882, 14084, 16578, 19391, 22548, 26074, 30000
        };

        /// <summary>
        /// Data for HP/MP formula
        /// First index is the job category (jobid / 100)
        /// Second index is 0 for levelup, 1 for hpmp ups
        /// Third index are the following:
        /// 0: HP min
        /// 1: HP max
        /// 2: Unknown
        /// 3: MP min
        /// 4: MP max
        /// 5: MP int stat multiplier
        /// </summary>
        public static short[,,] HpMpFormulaArguments = new short[6, 2, 6]
        {
            {{12, 16, 0, 10, 12, 20}, {8, 12, 0, 6, 8, 15}}, // Beginner
			{{24, 28, 0, 4, 6, 20}, {20, 24, 0, 2, 4, 15}}, // Warrior
			{{10, 14, 0, 22, 24, 20}, {6, 10, 0, 18, 20, 15}}, // Magician
			{{20, 24, 0, 14, 16, 20}, {16, 20, 0, 10, 12, 15}}, // Bowman
			{{20, 24, 0, 14, 16, 20}, {16, 20, 0, 10, 12, 15}}, // Thief
			{{20, 24, 0, 14, 16, 20}, {16, 20, 0, 10, 12, 15}}, // GM
        };

        public enum HpMpFormulaFields : int
        {
            HPMin = 0,
            HPMax = 1,
            Unk = 2,
            MPMin = 3,
            MPMax = 4,
            MPIntStatMultiplier = 5,
        }

        public static class EquipSlots
        {
            // Update this when we go to a newer version with new slots (looks like third job?)
            public const short MaxSlotIndex = (short)Slots.Pendant;

            public static bool IsValidEquipSlot(short slot)
            {
                if (slot < 0) slot = (short)-slot;
                if (slot > 100) slot -= 100;

                return slot > 0 && slot <= MaxSlotIndex;
            }

            public enum Slots
            {
                Invalid = 0,
                Helm = 1,
                Face = 2,
                Eye = 3,
                Earring = 4,
                Top = 5,
                Bottom = 6,
                Shoe = 7,
                Glove = 8,
                Cape = 9,
                Shield = 10,
                Weapon = 11,
                Ring1 = 12,
                Ring2 = 13,
                PetEquip1 = 14,
                Ring3 = 15,
                Ring4 = 16,
                Pendant = 17,
                Mount = 18,
                Saddle = 19,
                PetCollar = 20,
                PetLabelRing1 = 21,
                PetItemPouch1 = 22,
                PetMesoMagnet1 = 23,
                PetAutoHp = 24,
                PetAutoMp = 25,
                PetWingBoots1 = 26,
                PetBinoculars1 = 27,
                PetMagicScales1 = 28,
                PetQuoteRing1 = 29,
                PetEquip2 = 30,
                PetLabelRing2 = 31,
                PetQuoteRing2 = 32,
                PetItemPouch2 = 33,
                PetMesoMagnet2 = 34,
                PetWingBoots2 = 35,
                PetBinoculars2 = 36,
                PetMagicScales2 = 37,
                PetEquip3 = 38,
                PetLabelRing3 = 39,
                PetQuoteRing3 = 40,
                PetItemPouch3 = 41,
                PetMesoMagnet3 = 42,
                PetWingBoots3 = 43,
                PetBinoculars3 = 44,
                PetMagicScales3 = 45,
                PetItemIgnore1 = 46,
                PetItemIgnore2 = 47,
                PetItemIgnore3 = 48,
                Medal = 49,
                Belt = 50
            }
        }

        public static class Items
        {
            public const int PoisonousMushroom = 2011000;
            public const int PetMesoMagnet = 1812000;
            public const int PetItemPouch = 1812001;
            public const int PetAutoHp = 1812002;
            public const int PetAutoMp = 1812003;
            public const int PetWingBoots = 1812004;
            public const int PetBinoculars = 1812005;
            public const int PetMagicScales = 1812006;
            public const int Choco = 4090000; // 5110000 in newer versions

            public static class Types
            {
                public enum ItemTypes
                {
                    ArmorHelm = 100,
                    AccessoryFace = 101,
                    AccessoryEye = 102,
                    AccessoryEarring = 103,
                    ArmorTop = 104,
                    ArmorOverall = 105,
                    ArmorBottom = 106,
                    ArmorShoe = 107,
                    ArmorGlove = 108,
                    ArmorShield = 109,
                    ArmorCape = 110,
                    ArmorRing = 111,
                    ArmorPendant = 112,
                    Medal = 114,
                    Weapon1hSword = 130,
                    Weapon1hAxe = 131,
                    Weapon1hMace = 132,
                    WeaponDagger = 133,
                    WeaponWand = 137,
                    WeaponStaff = 138,
                    Weapon2hSword = 140,
                    Weapon2hAxe = 141,
                    Weapon2hMace = 142,
                    WeaponSpear = 143,
                    WeaponPolearm = 144,
                    WeaponBow = 145,
                    WeaponCrossbow = 146,
                    WeaponClaw = 147,
                    WeaponSkillFX = 160,
                    WeaponCash = 170,
                    PetEquip = 180,
                    PetSkills = 181,

                    ItemPotion = 200,
                    ItemSpecialPotion = 201, // Like drakes blood and poisonous mushroom
                    ItemFood = 202,
                    ItemReturnScroll = 203,
                    ItemScroll = 204,
                    ItemCure = 205,
                    ItemArrow = 206,
                    ItemStar = 207,
                    ItemMegaPhone = 208,
                    ItemWeather = 209,
                    ItemSummonBag = 210,
                    ItemPetTag = 211,
                    ItemPetFood = 212,
                    ItemKite = 213,
                    ItemMesoSack = 214,
                    ItemJukebox = 215,
                    ItemNote = 216,
                    ItemTeleportRock = 217,
                    ItemAPSPReset = 218,

                    Pet = 500,
                    SetupEventItem = 399,
                    EtcMetal = 401,
                    EtcMineral = 402,
                    EtcEmote = 404,
                    EtcCoupon = 405,
                    EtcStorePermit = 406,
                    EtcWaterOfLife = 407,
                    EtcOmokSet = 408,
                    EtcChocolate = 409,
                    EtcEXPCoupon = 410,
                    EtcGachaponTicket = 411,
                    EtcSafetyCharm = 412,
                    EtcForging = 413,
                }
            }

            public static class ScrollTypes
            {
                public enum Types
                {
                    Helm = 0,
                    Face = 100,
                    Eye = 200,
                    Earring = 300,
                    Topwear = 400,
                    Overall = 500,
                    Bottomwear = 600,
                    Shoes = 700,
                    Gloves = 800,
                    Shield = 900,
                    Cape = 1000,
                    Ring = 1100,
                    Pendant = 1200,
                    OneHandedSword = 3000,
                    OneHandedAxe = 3100,
                    OneHandedMace = 3200,
                    Dagger = 3300,
                    Wand = 3700,
                    Staff = 3800,
                    TwoHandedSword = 4000,
                    TwoHandedAxe = 4100,
                    TwoHandedMace = 4200,
                    Spear = 4300,
                    Polearm = 4400,
                    Bow = 4500,
                    Crossbow = 4600,
                    Claw = 4700,
                    PetEquip = 8000,
                }
            }
        }


        public static class MobSkills
        {
            public enum Skills
            {
                WeaponAttackUp = 100,
                WeaponAttackUpAoe = 110,
                MagicAttackUp = 101,
                MagicAttackUpAoe = 111,
                WeaponDefenseUp = 102,
                WeaponDefenseUpAoe = 112,
                MagicDefenseUp = 103,
                MagicDefenseUpAoe = 113,
                HealAoe = 114,
                SpeedUpAoe = 115,
                Seal = 120,
                Darkness = 121,
                Weakness = 122,
                Stun = 123,
                Curse = 124,
                Poison = 125,
                PoisonMist = 131,
                WeaponImmunity = 140,
                MagicImmunity = 141,
                Summon = 200
            }
        }

        public enum Element
        {
            Physical = 0x0,
            Ice = 0x1,
            Fire = 0x2,
            Light = 0x3,
            Poison = 0x4,
            Holy = 0x5,
            Dark = 0x6,
            Undead = 0x7
        }

        public static Element GetElementByChargedSkillID(int SkillID)
        {
            switch (SkillID)
            {
                case WhiteKnight.Skills.SwordFireCharge:
                case WhiteKnight.Skills.BwFireCharge:
                    return Element.Fire;
                case WhiteKnight.Skills.SwordLitCharge:
                case WhiteKnight.Skills.BwLitCharge:
                    return Element.Light;
                case WhiteKnight.Skills.SwordIceCharge:
                case WhiteKnight.Skills.BwIceCharge:
                    return Element.Ice;
                default:
                    return Element.Physical;
            }
        }

        public static byte getItemTypeInPacket(int itemid)
        {
            if (isEquip(itemid)) return 1;
            else if (isPet(itemid)) return 5;
            else return 2;
        }

        public static string getDropName(int objectid, bool isMob)
        {
            return (isMob ? "m" : "r") + objectid.ToString();
        }

        public static short getSkillJob(int skillId) => (short)(skillId / 10000);
        public static byte getInventory(int itemid) { return (byte)(itemid / 1000000); }
        public static int getItemType(int itemid) { return (itemid / 10000); }
        public static int getScrollType(int itemid) { return ((itemid % 10000) - (itemid % 100)); }
        public static int itemTypeToScrollType(int itemid) { return ((getItemType(itemid) % 100) * 100); }
        public static bool isArrow(int itemid) { return (getItemType(itemid) == (int)Items.Types.ItemTypes.ItemArrow); }
        public static bool isStar(int itemid) { return (getItemType(itemid) == (int)Items.Types.ItemTypes.ItemStar); }
        public static bool isRechargeable(int itemid) { return isStar(itemid); }
        public static bool isEquip(int itemid) { return (getInventory(itemid) == 1); }
        public static bool isPet(int itemid) { return (getInventory(itemid) == 5); }
        public static bool isStackable(int itemid) { return !(isRechargeable(itemid) || isEquip(itemid) || isPet(itemid)); }
        public static bool isOverall(int itemid) { return (getItemType(itemid) == (int)Items.Types.ItemTypes.ArmorOverall); }
        public static bool isTop(int itemid) { return (getItemType(itemid) == (int)Items.Types.ItemTypes.ArmorTop); }
        public static bool isBottom(int itemid) { return (getItemType(itemid) == (int)Items.Types.ItemTypes.ArmorBottom); }
        public static bool isShield(int itemid) { return (getItemType(itemid) == (int)Items.Types.ItemTypes.ArmorShield); }
        public static bool is2hWeapon(int itemid) { return (getItemType(itemid) / 10 == 14); }
        public static bool is1hWeapon(int itemid) { return (getItemType(itemid) / 10 == 13); }
        public static bool isBow(int itemid) { return (getItemType(itemid) == (int)Items.Types.ItemTypes.WeaponBow); }
        public static bool isCrossbow(int itemid) { return (getItemType(itemid) == (int)Items.Types.ItemTypes.WeaponCrossbow); }
        public static bool isSword(int itemid) { return (getItemType(itemid) == (int)Items.Types.ItemTypes.Weapon1hSword || getItemType(itemid) == (int)Items.Types.ItemTypes.Weapon2hSword); }
        public static bool isMace(int itemid) { return (getItemType(itemid) == (int)Items.Types.ItemTypes.Weapon1hMace || getItemType(itemid) == (int)Items.Types.ItemTypes.Weapon2hMace); }
        public static bool isValidInventory(byte inv) { return (inv > 0 && inv <= 5); }

        public static bool isPuppet(int skillid) { return (skillid == Sniper.Skills.Puppet || skillid == Ranger.Skills.Puppet); }
        public static bool isSummon(int skillid) { return (isPuppet(skillid) || skillid == Priest.Skills.SummonDragon || skillid == Ranger.Skills.SilverHawk || skillid == Sniper.Skills.GoldenEagle); }

        public static byte getMasteryDisplay(byte level) { return (byte)((level + 1) / 2); }

        public static short getJobTrack(short job, bool flatten = false) { return (short)(flatten ? ((job / 100) % 10) : (job / 100)); }

        public static EquipSlots.Slots GetBodyPartFromItem(int ItemID)
        {
            var ItemBodyPart = ItemID / 10000;
            var ItemBodyPartEX = ItemBodyPart / 10;

            if (ItemID / 10000 > 119)
            {
                if (ItemBodyPart == 180)
                    return EquipSlots.Slots.PetEquip1;

                if (ItemBodyPart == 181)
                {
                    switch (ItemID)
                    {
                        case Items.PetMesoMagnet: return EquipSlots.Slots.PetMesoMagnet1;
                        case Items.PetItemPouch: return EquipSlots.Slots.PetItemPouch1;
                        case Items.PetAutoHp: return EquipSlots.Slots.PetAutoHp;
                        case Items.PetAutoMp: return EquipSlots.Slots.PetAutoMp;
                        case Items.PetWingBoots: return EquipSlots.Slots.PetWingBoots1;
                        case Items.PetBinoculars: return EquipSlots.Slots.PetBinoculars1;
                        case Items.PetMagicScales: return EquipSlots.Slots.PetMagicScales1;
                    }
                }
                else if (ItemBodyPart != 182)
                {
                    switch (ItemBodyPart)
                    {
                        case 183: return EquipSlots.Slots.PetQuoteRing1;
                        case 190: return EquipSlots.Slots.Mount;
                        case 191: return EquipSlots.Slots.Saddle;
                        case 192: return EquipSlots.Slots.PetCollar;
                        default:
                            if (ItemBodyPartEX != 13 && ItemBodyPartEX != 14 && ItemBodyPartEX != 16 && ItemBodyPartEX != 17)
                                return EquipSlots.Slots.Invalid;
                            return EquipSlots.Slots.Weapon;
                    }
                }
                return EquipSlots.Slots.PetLabelRing1;
            }
            switch (ItemBodyPart)
            {
                case 100: return EquipSlots.Slots.Helm;
                case 101: return EquipSlots.Slots.Face;
                case 102: return EquipSlots.Slots.Eye;
                case 103: return EquipSlots.Slots.Earring;
                case 104: return EquipSlots.Slots.Top; //Could do a fallthrough but that would look weird
                case 105: return EquipSlots.Slots.Top;
                case 106: return EquipSlots.Slots.Bottom;
                case 107: return EquipSlots.Slots.Shoe;
                case 108: return EquipSlots.Slots.Glove;
                case 110: return EquipSlots.Slots.Cape;
                case 111: return EquipSlots.Slots.Ring1;//When this is returned keep in mind there are 4 slots and this is only 1 of them
                case 112: return EquipSlots.Slots.Pendant;
                case 119: return EquipSlots.Slots.Shield;
                case 109: return EquipSlots.Slots.Shield;
                default:
                    break;
            }
            if (ItemBodyPartEX != 13 && ItemBodyPartEX != 14 && ItemBodyPartEX != 16 && ItemBodyPartEX != 17)
                return EquipSlots.Slots.Invalid;
            return EquipSlots.Slots.Weapon;
        }

        public static bool IsCorrectBodyPart(int ItemID, short BodyPart, byte Gender)
        {
            var ItemBodyPart = ItemID / 10000;
            var ItemBodyPartEX = ItemBodyPart / 10;
            
            if (ItemBodyPart <= 119)
            {
                if (ItemBodyPart != 119)
                {
                    switch (ItemBodyPart)
                    {
                        case 100:
                            return BodyPart == 1;
                        case 101:
                            return BodyPart == 2;
                        case 102:
                            return BodyPart == 3;
                        case 103:
                            return BodyPart == 4;
                        case 104:
                        case 105:
                            return BodyPart == 5;
                        case 106:
                            return BodyPart == 6;
                        case 107:
                            return BodyPart == 7;
                        case 108:
                            return BodyPart == 8;
                        case 110:
                            return BodyPart == 9;
                        case 111:
                            if (BodyPart == 12 || BodyPart == 13 || BodyPart == 15)
                                return true;
                            return BodyPart == 16;
                        case 112:
                            if (BodyPart == 17)
                                return true;
                            return BodyPart == 30;
                        case 113:
                            return BodyPart == 22;
                        case 114:
                            return BodyPart == 21;
                        case 115:
                            return BodyPart == 23;
                        case 109:
                            return BodyPart == 10;
                        default:
                            {
                                if (ItemBodyPartEX == 13 || ItemBodyPartEX == 14 || ItemBodyPartEX == 16 || ItemBodyPartEX == 17)
                                    return BodyPart == 11;
                                return false;
                            }
                    }
                }
                return BodyPart == 10;
            }
            if (ItemBodyPart > 190)
            {
                if (ItemBodyPart == 191)
                    return BodyPart == 19;
                if (ItemBodyPart == 192)
                    return BodyPart == 20;
                if (ItemBodyPart == 194)
                    return BodyPart == 1000;
                if (ItemBodyPart == 195)
                    return BodyPart == 1001;
                if (ItemBodyPart == 196)
                    return BodyPart == 1002;
                if (ItemBodyPart == 197)
                    return BodyPart == 1003;
                if (ItemBodyPartEX == 13 || ItemBodyPartEX == 14 || ItemBodyPartEX == 16 || ItemBodyPartEX == 17)
                    return BodyPart == 11;
                return false;
            }
            if (ItemBodyPart == 190)
                return BodyPart == 18;
            if (ItemBodyPart == 134)
                return BodyPart == 10;
            switch (ItemBodyPart)
            {
                case 161:
                    return BodyPart == 1100;
                case 162:
                    return BodyPart == 1101;
                case 163:
                    return BodyPart == 1102;
                case 164:
                    return BodyPart == 1103;
                default:
                    if (ItemBodyPart != 165)
                    {
                        if (ItemBodyPart == 180)
                        {
                            if (BodyPart == 14 || BodyPart == 24)
                                return true;
                            return BodyPart == 25;
                        }

                        if (ItemBodyPartEX == 13 || ItemBodyPartEX == 14 || ItemBodyPartEX == 16 || ItemBodyPartEX == 17)
                            return BodyPart == 11;
                        return false;
                    }
                    return BodyPart == 1104;
            }
        }

        public static int GetLevelEXP(byte level)
        {
            if (level >= 200) return 0;
            return EXP[level - 1];
        }

        public static class JobTracks
        {
            public enum Tracks
            {
                Beginner = 0,
                Warrior = 1,
                Magician = 2,
                Bowman = 3,
                Thief = 4,
                Gm = 5,
            }
        }

        public static class Swordsman
        {
            public const short ID = 100;
            public static class Skills
            {
                public const int ImprovedMaxHpIncrease = 1000001;
                public const int Endure = 1000002;
                public const int IronBody = 1001003;
            }
        }
        public static class Fighter
        {
            public const int ID = 110;
            public static class Skills
            {
                public const int AxeBooster = 1101005;
                public const int AxeMastery = 1100001;
                public const int PowerGuard = 1101007;
                public const int Rage = 1101006;
                public const int SwordBooster = 1101004;
                public const int SwordMastery = 1100000;
            }
        }
        public static class Crusader
        {
            public const short ID = 111;
            public static class Skills
            {
                public const int ImprovedMpRecovery = 1110000;
                public const int ArmorCrash = 1111007;
                public const int AxeComa = 1111006;
                public const int AxePanic = 1111004;
                public const int ComboAttack = 1111002;
                public const int Shout = 1111008;
                public const int SwordComa = 1111005;
                public const int SwordPanic = 1111003;
            }
        }
        public static class Page
        {
            public const short ID = 120;
            public static class Skills
            {
                public const int BwBooster = 1201005;
                public const int BwMastery = 1200001;
                public const int PowerGuard = 1201007;
                public const int SwordBooster = 1201004;
                public const int SwordMastery = 1200000;
                public const int Threaten = 1201006;
            }
        }
        public static class WhiteKnight
        {
            public const short ID = 121;
            public static class Skills
            {
                public const int ImprovedMpRecovery = 1210000;
                public const int BwFireCharge = 1211004;
                public const int BwIceCharge = 1211006;
                public const int BwLitCharge = 1211008;
                public const int ChargeBlow = 1211002;
                public const int MagicCrash = 1211009;
                public const int SwordFireCharge = 1211003;
                public const int SwordIceCharge = 1211005;
                public const int SwordLitCharge = 1211007;
            }
        }
        public static class Spearman
        {
            public const short ID = 130;
            public static class Skills
            {
                public const int HyperBody = 1301007;
                public const int IronWill = 1301006;
                public const int PolearmBooster = 1301005;
                public const int PolearmMastery = 1300001;
                public const int SpearBooster = 1301004;
                public const int SpearMastery = 1300000;
            }
        }
        public static class DragonKnight
        {
            public const short ID = 131;
            public static class Skills
            {
                public const int DragonBlood = 1311008;
                public const int DragonRoar = 1311006;
                public const int ElementalResistance = 1310000;
                public const int PowerCrash = 1311007;
                public const int Sacrifice = 1311005;
            }
        }
        public static class Magician
        {
            public const short ID = 200;
            public static class Skills
            {
                public const int ImprovedMpRecovery = 2000000;
                public const int ImprovedMaxMpIncrease = 2000001;
                public const int MagicArmor = 2001003;
                public const int MagicGuard = 2001002;
                public const int MagicClaw = 2001005;
                public const int EnergyBolt = 2001004;
            }
        }
        public static class FPWizard
        {
            public const short ID = 210;
            public static class Skills
            {
                public const int Meditation = 2101001;
                public const int MpEater = 2100000;
                public const int PoisonBreath = 2101005;
                public const int FireArrow = 2101004;
                public const int Slow = 2101003;
            }
        }
        public static class FPMage
        {
            public const short ID = 211;
            public static class Skills
            {
                public const int ElementAmplification = 2110001;
                public const int ElementComposition = 2111006;
                public const int PartialResistance = 2110000;
                public const int PoisonMyst = 2111003;
                public const int Seal = 2111004;
                public const int SpellBooster = 2111005;
                public const int Explosion = 2111002;
            }
        }
        public static class ILWizard
        {
            public const short ID = 220;
            public static class Skills
            {
                public const int ColdBeam = 2201004;
                public const int Meditation = 2201001;
                public const int MpEater = 2200000;
                public const int Slow = 2201003;
                public const int ThunderBolt = 2201005;
            }
        }
        public static class ILMage
        {
            public const short ID = 221;
            public static class Skills
            {
                public const int ElementAmplification = 2210001;
                public const int ElementComposition = 2211006;
                public const int IceStrike = 2211002;
                public const int PartialResistance = 2210000;
                public const int Seal = 2211004;
                public const int SpellBooster = 2211005;
                public const int Lightening = 2211003;
            }
        }
        public static class Cleric
        {
            public const short ID = 230;
            public static class Skills
            {
                public const int Bless = 2301004;
                public const int Heal = 2301002;
                public const int Invincible = 2301003;
                public const int MpEater = 2300000;
                public const int HolyArrow = 2301005;
            }
        }
        public static class Priest
        {
            public const short ID = 231;
            public static class Skills
            {
                public const int Dispel = 2311001;
                public const int Doom = 2311005;
                public const int ElementalResistance = 2310000;
                public const int HolySymbol = 2311003;
                public const int MysticDoor = 2311002;
                public const int SummonDragon = 2311006;
            }
        }
        public static class Archer
        {
            public const short ID = 300;
            public static class Skills
            {
                public const int BlessingOfAmazon = 3000000;
                public const int CriticalShot = 3000001;
                public const int Focus = 3001003;
            }
        }
        public static class Hunter
        {
            public const short ID = 310;
            public static class Skills
            {
                public const int PowerKnockback = 3101003;
                public const int ArrowBomb = 3101005;
                public const int BowBooster = 3101002;
                public const int BowMastery = 3100000;
                public const int SoulArrow = 3101004;
            }
        }
        public static class Ranger
        {
            public const short ID = 311;
            public static class Skills
            {
                public const int MortalBlow = 3110001;
                public const int Puppet = 3111002;
                public const int SilverHawk = 3111005;
            }
        }
        public static class Crossbowman
        {
            public const short ID = 320;
            public static class Skills
            {
                public const int PowerKnockback = 3201003;
                public const int CrossbowBooster = 3201002;
                public const int CrossbowMastery = 3200000;
                public const int SoulArrow = 3201004;
            }
        }
        public static class Sniper
        {
            public const short ID = 321;
            public static class Skills
            {
                public const int Blizzard = 3211003;
                public const int GoldenEagle = 3211005;
                public const int MortalBlow = 3210001;
                public const int Puppet = 3211002;
            }
        }
        public static class Rogue
        {
            public const short ID = 400;
            public static class Skills
            {
                public const int NimbleBody = 4000000;
                public const int DarkSight = 4001003;
                public const int Disorder = 4001002;
                public const int DoubleStab = 4001334;
                public const int LuckySeven = 4001344;
            }
        }
        public static class Assassin
        {
            public const short ID = 410;
            public static class Skills
            {
                public const int ClawBooster = 4101003;
                public const int ClawMastery = 4100000;
                public const int CriticalThrow = 4100001;
                public const int Endure = 4100002;
                public const int Drain = 4101005;
                public const int Haste = 4101004;
            }
        }
        public static class Hermit
        {
            public const short ID = 411;
            public static class Skills
            {
                public const int Alchemist = 4110000;
                public const int Avenger = 4111005;
                public const int MesoUp = 4111001;
                public const int ShadowMeso = 4111004;
                public const int ShadowPartner = 4111002;
                public const int ShadowWeb = 4111003;
            }
        }
        public static class Bandit
        {
            public const short ID = 420;
            public static class Skills
            {
                public const int DaggerBooster = 4201002;
                public const int DaggerMastery = 4200000;
                public const int Endure = 4200001;
                public const int Haste = 4201003;
                public const int SavageBlow = 4201005;
                public const int Steal = 4201004;
            }
        }
        public static class ChiefBandit
        {
            public const short ID = 421;
            public static class Skills
            {
                public const int Assaulter = 4211002;
                public const int BandOfThieves = 4211004;
                public const int Chakra = 4211001;
                public const int MesoExplosion = 4211006;
                public const int MesoGuard = 4211005;
                public const int Pickpocket = 4211003;
            }
        }
        public static class Gm
        {
            public const short ID = 500;
            public static class Skills
            {
                public const int Bless = 5001003;
                public const int Haste = 5001001;
                public const int HealPlusDispell = 5001000;
                public const int Hide = 5001004;
                public const int HolySymbol = 5001002;
                public const int Resurrection = 5001005;
                public const int SuperDragonRoar = 5001006;
                public const int Teleport = 5001007;

                public const int ItemExplosion = 5001008;
                public const int ShadowPartner = 5001009;
                public const int JumpDown = 50010010;
            }
        }
    }
}