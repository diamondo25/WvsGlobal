using System;
using System.Collections.Generic;
using WvsBeta.Common;

namespace WvsBeta.Game
{
    [Flags]
    public enum BuffValueTypes : uint
    {
        // Byte 1
        WeaponAttack = 0x01,
        WeaponDefense = 0x02,
        MagicAttack = 0x04,
        MagicDefense = 0x08,

        Accurancy = 0x10,
        Avoidability = 0x20,
        Hands = 0x40, // Yes, this has a modifier too.
        Speed = 0x80,

        // Byte 2
        Jump = 0x100,
        MagicGuard = 0x200,
        DarkSight = 0x400,
        Booster = 0x800,

        PowerGuard = 0x1000,
        MaxHP = 0x2000,
        MaxMP = 0x4000,
        Invincible = 0x8000,

        // Byte 3
        SoulArrow = 0x10000,
        Stun = 0x020000, // Mob Skill: Stun and Dragon Roar
        Poison = 0x40000, // Mob Skill: Poison
        Seal = 0x80000, // Mob Skill: Seal

        Darkness = 0x100000, // Mob Skill: Darkness
        ComboAttack = 0x200000,
        Charges = 0x400000,
        DragonBlood = 0x800000,

        // Byte 4
        HolySymbol = 0x1000000,
        MesoUP = 0x2000000,
        ShadowPartner = 0x4000000,
        PickPocketMesoUP = 0x8000000,

        MesoGuard = 0x10000000,
        Thaw = 0x20000000,
        Weakness = 0x40000000, // Mob Skill: Weakness
        Curse = 0x80000000, // Mob Skill: Curse

        ALL = 0xFFFFFFFF,
        SPEED_BUFF_ELEMENT = Speed | Jump | Stun | Weakness,
    }

    public class BuffDataProvider
    {
        public static readonly Dictionary<int, BuffValueTypes> SkillBuffValues = new Dictionary<int, BuffValueTypes>
        {
            {Constants.Fighter.Skills.AxeBooster, BuffValueTypes.Booster},
            {Constants.Fighter.Skills.SwordBooster, BuffValueTypes.Booster},
            {Constants.Page.Skills.BwBooster, BuffValueTypes.Booster},
            {Constants.Page.Skills.SwordBooster, BuffValueTypes.Booster},
            {Constants.Spearman.Skills.SpearBooster, BuffValueTypes.Booster},
            {Constants.Spearman.Skills.PolearmBooster, BuffValueTypes.Booster},
            {Constants.FPMage.Skills.SpellBooster, BuffValueTypes.Booster},
            {Constants.ILMage.Skills.SpellBooster, BuffValueTypes.Booster},
            {Constants.Hunter.Skills.BowBooster, BuffValueTypes.Booster},
            {Constants.Crossbowman.Skills.CrossbowBooster, BuffValueTypes.Booster},
            {Constants.Assassin.Skills.ClawBooster, BuffValueTypes.Booster},
            {Constants.Bandit.Skills.DaggerBooster, BuffValueTypes.Booster},

            {Constants.Magician.Skills.MagicGuard, BuffValueTypes.MagicGuard},

            {Constants.Magician.Skills.MagicArmor, BuffValueTypes.WeaponDefense},
            {Constants.Swordsman.Skills.IronBody, BuffValueTypes.WeaponDefense},

            {Constants.Archer.Skills.Focus, BuffValueTypes.Accurancy | BuffValueTypes.Avoidability},
            //{ (int)Constants.Archer.Skills.Focus,  BuffValueTypes.Avoidability },



            {Constants.Fighter.Skills.Rage, BuffValueTypes.WeaponAttack | BuffValueTypes.WeaponDefense},

            {Constants.Fighter.Skills.PowerGuard, BuffValueTypes.PowerGuard},
            {Constants.Page.Skills.PowerGuard, BuffValueTypes.PowerGuard},

            {Constants.Spearman.Skills.IronWill, BuffValueTypes.WeaponDefense | BuffValueTypes.MagicDefense},

            {Constants.Spearman.Skills.HyperBody, BuffValueTypes.MaxHP | BuffValueTypes.MaxMP},

            {Constants.FPWizard.Skills.Meditation, BuffValueTypes.MagicAttack},
            {Constants.ILWizard.Skills.Meditation, BuffValueTypes.MagicAttack},

            {Constants.Cleric.Skills.Invincible, BuffValueTypes.Invincible},

            {
                Constants.Cleric.Skills.Bless,
                BuffValueTypes.WeaponDefense | BuffValueTypes.MagicDefense | BuffValueTypes.Accurancy |
                BuffValueTypes.Avoidability
            },
            {
                Constants.Gm.Skills.Bless,
                BuffValueTypes.WeaponAttack | BuffValueTypes.WeaponDefense | BuffValueTypes.MagicAttack |
                BuffValueTypes.MagicDefense | BuffValueTypes.Accurancy | BuffValueTypes.Avoidability
            },

            {Constants.ChiefBandit.Skills.MesoGuard, BuffValueTypes.MesoGuard},

            {Constants.Priest.Skills.HolySymbol, BuffValueTypes.HolySymbol},
            {Constants.Gm.Skills.HolySymbol, BuffValueTypes.HolySymbol},

            {Constants.ChiefBandit.Skills.Pickpocket, BuffValueTypes.PickPocketMesoUP},
            {Constants.Hermit.Skills.MesoUp, BuffValueTypes.PickPocketMesoUP},

            {Constants.DragonKnight.Skills.DragonRoar, BuffValueTypes.Stun},

            {Constants.WhiteKnight.Skills.BwFireCharge, BuffValueTypes.MagicAttack | BuffValueTypes.Charges},
            {Constants.WhiteKnight.Skills.BwIceCharge, BuffValueTypes.MagicAttack | BuffValueTypes.Charges},
            {Constants.WhiteKnight.Skills.BwLitCharge, BuffValueTypes.MagicAttack | BuffValueTypes.Charges},
            {Constants.WhiteKnight.Skills.SwordFireCharge, BuffValueTypes.MagicAttack | BuffValueTypes.Charges},
            {Constants.WhiteKnight.Skills.SwordIceCharge, BuffValueTypes.MagicAttack | BuffValueTypes.Charges},
            {Constants.WhiteKnight.Skills.SwordLitCharge, BuffValueTypes.MagicAttack | BuffValueTypes.Charges},

            {Constants.Assassin.Skills.Haste, BuffValueTypes.Speed | BuffValueTypes.Jump},
            {Constants.Bandit.Skills.Haste, BuffValueTypes.Speed | BuffValueTypes.Jump},
            {Constants.Gm.Skills.Haste, BuffValueTypes.Speed | BuffValueTypes.Jump},

            {Constants.Rogue.Skills.DarkSight, BuffValueTypes.Speed | BuffValueTypes.DarkSight},
            {Constants.Gm.Skills.Hide, BuffValueTypes.Invincible | BuffValueTypes.DarkSight},

            {Constants.Hunter.Skills.SoulArrow, BuffValueTypes.SoulArrow},
            {Constants.Crossbowman.Skills.SoulArrow, BuffValueTypes.SoulArrow},

            {Constants.Hermit.Skills.ShadowPartner, BuffValueTypes.ShadowPartner},
            {Constants.Gm.Skills.ShadowPartner, BuffValueTypes.ShadowPartner},

            {Constants.Crusader.Skills.ComboAttack, BuffValueTypes.ComboAttack},

            {Constants.DragonKnight.Skills.DragonBlood, BuffValueTypes.WeaponAttack | BuffValueTypes.DragonBlood},
        };
    }
}