using System;
using WvsBeta.Common;

namespace WvsBeta.Game
{
    public static class DamageFormula
    {
        public static MessagePacket.MessageTypes GetGMNoticeType(int damageGiven, int maxDamageCalculated)
        {
            int percent = damageGiven * 100 / maxDamageCalculated;
            if (percent < 115) return MessagePacket.MessageTypes.Notice;
            if (percent < 150) return MessagePacket.MessageTypes.Notice;
            if (percent < 200) return MessagePacket.MessageTypes.RedText;
            return MessagePacket.MessageTypes.Megaphone;
        }

        // Author: wackyracer / Joren McGrew
        // Checks for points in Critical Throw/Shot and returns the damage modifier appropriately.
        // +55% ~ +200% damage multiplier based on rank of Critical Throw/Shot, otherwise, return normal damage.
        public static double CriticalStrikeModifier(Character chr)
        {
            try
            {
                var chrCriticalShot = chr.Skills.GetSkillLevelData(3000001, out byte critShotLevel);
                var chrCriticalThrow = chr.Skills.GetSkillLevelData(4100001, out byte critThrowLevel);

                // If they are an Assassin branch of the Thief class and have at least one point Critical Throw
                if ((chr.PrimaryStats.Job / 10 == 41 || chr.PrimaryStats.Job / 100 == 5) && chrCriticalThrow != null)
                {
                    return (double)(chrCriticalThrow.Damage / 100.0);
                }

                // If they are a Bowman class and have at least one point Critical Shot
                else if ((chr.PrimaryStats.Job / 100 == 3 || chr.PrimaryStats.Job / 100 == 5) && chrCriticalThrow != null)
                {
                    return (double)(chrCriticalShot.Damage / 100.0);
                }

                return 1.0;
            }

            catch (Exception bigkrit)
            {
                Program.MainForm.LogAppend("DAMAGEFORMULA->'CRIT' EXCEPTION: " + bigkrit);
                return 1.0;
            }
        }

        // Author: wackyracer / Joren McGrew
        // Checks for points in Final Attack and returns the damage modifier appropriately.
        // +5% ~ +150% damage multiplier based on rank of Critical Throw/Shot, otherwise, return normal damage.
        public static double FinalAttackModifier(Character chr, int SkillID)
        {
            var chrFinalAttack = chr.Skills.GetSkillLevelData(SkillID, out byte FinalAttackLevel);

            // If their job ID matches first 3 digits of the Final Attack Skill ID, and has a skill point in it, then it is a legal operation.
            if (chr.PrimaryStats.Job == (SkillID / 10000) && chrFinalAttack != null)
            {
                return (double)(chrFinalAttack.Damage / 100.0);
            }

            return 1.0;
        }

        // Author: wackyracer / Joren McGrew
        // Checks for a level disadvantage between the mob and the player.
        // If there is, properly reduce damage output, otherwise don't.
        // Formula is -1% damage multiplied by the level difference.
        public static double LevelDisadvantageModifierDmg(int CharLevel, int MobLevel)
        {
            if (MobLevel > CharLevel)
            {
                if (MobLevel - CharLevel < 100)
                {
                    return 1.0 - 0.01 * (double)(MobLevel - CharLevel);
                }

                else
                {
                    return 0.0;
                }
            }

            return 1.0;
        }

        // Author: wackyracer / Joren McGrew
        // Checks for a level disadvantage between the mob and the player.
        // If there is, properly reduce damage output, otherwise don't.
        // Formula is -1% damage multiplied by the level difference.
        public static double LevelDisadvantageModifierAcc(int CharLevel, int MobLevel)
        {
            if (MobLevel > CharLevel)
            {
                if (MobLevel - CharLevel < 100)
                {
                    return 1.0 - 0.01 * (double)(MobLevel - CharLevel);
                }

                else
                {
                    return 0.0;
                }
            }

            return 0.0;
        }

        // Author: wackyracer / Joren Mcgrew & Exile / Max Anderson
        // Gets the damage amplification for elemental attacks on a mob.
        // Currently only handles 2nd job skills.
        public static double ElementModifier(Mob mob, int SkillID)
        {
            switch (SkillID)
            {
                case (int)Constants.FPWizard.Skills.FireArrow:
                    return CheckMobAttr(mob.Data.elemAttr, 'F');
                case (int)Constants.FPWizard.Skills.PoisonBreath:
                    return CheckMobAttr(mob.Data.elemAttr, 'S');
                case (int)Constants.ILWizard.Skills.ColdBeam:
                    return CheckMobAttr(mob.Data.elemAttr, 'I');
                case (int)Constants.ILWizard.Skills.ThunderBolt:
                    return CheckMobAttr(mob.Data.elemAttr, 'L');
                case (int)Constants.Cleric.Skills.Heal:
                case (int)Constants.Cleric.Skills.HolyArrow:
                    return CheckMobAttr(mob.Data.elemAttr, 'H');
                default:
                    return 1.0;
            }
        }

        // Authors: wackyracer / Joren McGrew & Rath111 / Rod Jalali
        // Checks the mob's elemental attribute located in the WZ files.
        // If there is one, it returns the appropriate damage amplification / reduction / nullification. Otherwise, normal damage is sent.
        // ELEMENTAL TYPES:
        // F = Fire
        // I = Ice
        // S = Poison
        // L = Lightning
        // H = Holy
        // MODES:
        // 0 = normal damage
        // 1 = nullifies damage
        // 2 = halves damage
        // 3 = 1.5x damage
        // Elements in the WZ come in pair(s) of two.
        private static double CheckMobAttr(string sequence, char elementToCheck)
        {
            if (sequence != null && sequence.Length % 2 != 1)
            {
                for (int i = 0; i < sequence.Length / 2; i += 2)
                {
                    char elementLabel = sequence[i];

                    if (elementLabel == elementToCheck)
                    {
                        char rating = sequence[i + 1];

                        switch (rating)
                        {
                            case '0': // Deals normal damage
                            {
                                return 1.0;
                            }

                            case '1': // Nullifies damage
                            {
                                return 0.0;
                            }

                            case '2': // Halves damage
                            {
                                return 0.5;
                            }

                            case '3': // 1.5x damage
                            {
                                return 1.5;
                            }
                        }
                    }
                }
            }

            return 1.0;
        }

        // Author: wackyracer / Joren McGrew
        // Fetches the Mastery of a given character and returns it as a double.
        // This function is primarily used for assisting calculations in other functions.
        public static double GetMastery(Character chr)
        {
            // Abbreviation description:
            // First letter is the first letter of the job it is relative to. F = Fighter, P = Page, and so on.
            // Second letter is the weapon type. S = Sword, A = Axe, and so on.
            // Third letter is always M for Mastery.
            double FSM = chr.Skills.GetSkillLevel(1100000);
            double FAM = chr.Skills.GetSkillLevel(1100001);
            double PSM = chr.Skills.GetSkillLevel(1200000);
            double PBM = chr.Skills.GetSkillLevel(1200001);
            double SSM = chr.Skills.GetSkillLevel(1300000);
            double SPM = chr.Skills.GetSkillLevel(1300001);
            double HBM = chr.Skills.GetSkillLevel(3100000);
            double CCM = chr.Skills.GetSkillLevel(3200000);
            double ACM = chr.Skills.GetSkillLevel(4100000);
            double BDM = chr.Skills.GetSkillLevel(4200000);

            switch (chr.Job)
            {
                case 110:
                    return FSM > FAM ? FSM : FAM;
                case 120:
                    return PSM > PBM ? PSM : PBM;
                case 130:
                    return SSM > SPM ? SSM : SPM;
                case 310:
                    return HBM;
                case 320:
                    return CCM;
                case 410:
                    return ACM;
                case 420:
                    return BDM;
                default:
                    return 0.0;
            }
        }

        // Author: wackyracer / Joren McGrew
        // This formula checks for the character's chances to hit a particular mob.
        // This formula is from: https://ayumilovemaple.wordpress.com/2009/09/06/maplestory-formula-compilation/
        // Accuracy = (DEX*0.8) + (LUK*0.5) + (accuracy from mastery and equipment)
        // Chance to Hit = Accuracy/((1.84 + 0.07 * D) * Avoid) - 1
        // (D = monster level - your level.If negative, make it 0.)
        public static double CalcPhysicalAcc(Character chr, Mob mob)
        {
            double chrDEX = chr.PrimaryStats.GetDexAddition();
            double chrLUK = chr.PrimaryStats.GetLukAddition();
            double chrBonuses = chr.PrimaryStats.BuffAccurancy.N + chr.Inventory.GetTotalAccInEquips() + GetMastery(chr);
            double chrACC = (chrDEX * 0.8) + (chrLUK * 0.5) + chrBonuses;
            double ChanceToHit = chrACC / ((1.84 + 0.07 * LevelDisadvantageModifierAcc(chr.Level, mob.Level)) * mob.Data.Eva) - 1.0;

            return ChanceToHit;
        }

        // Author: wackyracer / Joren McGrew
        // This formula retrieves the type of weapon that the character is currently using to attack with.
        public static string GetWeaponType(Character chr)
        {
            var eqp = chr.Inventory.GetEquippedItemId(Constants.EquipSlots.Slots.Weapon, false);

            if (eqp == 0)
            {
                return "NONE";
            }

            else
            {
                switch (eqp / 1000)
                {
                    case 1302: // 1H SWORD
                        return "1HS";
                    case 1312: // 1H AXE
                        return "1HA";
                    case 1322: // 1H BLUNT WEAPON
                        return "1HBW";
                    case 1332: // DAGGER
                        if ((chr.PrimaryStats.Job / 100) == 4) { return "DAGGERT"; }
                        else { return "DAGGERNT"; }
                    case 1372: // WAND
                        return "WAND";
                    case 1382: // STAFF
                        return "STAFF";
                    case 1402: // 2H SWORD
                        return "2HS";
                    case 1412: // 2H AXE
                        return "2HA";
                    case 1422: // 2H BLUNT WEAPON
                        return "2HBW";
                    case 1432: // SPEAR
                        return "SPEAR";
                    case 1442: // POLE ARM
                        return "PA";
                    case 1452: // BOW
                        return "BOW";
                    case 1462: // CROSSBOW
                        return "XBOW";
                    case 1472: // CLAW
                        return "CLAW";
                    case 1602: // CASH EFFECT
                        return "CSE";
                    case 1702: // CASH WEAPON
                        return "CSW";
                }
            }

            return "NONE";
        }

        // Author: wackyracer / Joren McGrew
        // This is the formula for maximum melee damage dealt by any class.
        // TODO: This formula does not take the 3rd job skills into account yet. TODO
        // This formula is from: https://web.archive.org/web/20081009190310/http://www.southperry.net:80/forums/showthread.php?t=855&page=2
        // Formula Name: General Formula
        // MAX = (Primary Stat + Secondary Stat) * Weapon Attack / 100
        // I have added elemental damage amplification/reduction to the formula.
        // I have added mob resistance calculation for damage reduction to the formula.
        // I have added mob level difference from character (if mob level is higher than character's).
        // My formula looks like...
        // MAX = ((Primary Stat + Secondary Stat) * Weapon Attack / 100) * Element Modifier * Level Difference - (Mob Weapon Defense * 0.5)
        public static double MaximumMeleeDamage(Character chr, Mob mob, int Targets = 1, int SkillID = 99)
        {
            var MeleeSkill = chr.Skills.GetSkillLevelData(SkillID, out byte MeleeSkillLevel);
            string WeaponType;
            double WeaponAttack = chr.PrimaryStats.BuffWeaponAttack.N + chr.Inventory.GetTotalWAttackInEquips(false);

            try // FAILSAFE
            {
                WeaponType = GetWeaponType(chr); // What weapon was used? Sword, Axe, etc.?
                double PrimaryStat = 0.0;
                double SecondaryStat = 0.0;

                switch (WeaponType)
                {
                    case "1HS": // ONE HANDED SWORD
                        PrimaryStat = chr.PrimaryStats.GetStrAddition() * 4.0;
                        SecondaryStat = chr.PrimaryStats.GetDexAddition();
                    break;
                    case "1HA": // ONE HANDED AXE
                    case "1HBW": // ONE HANDED BLUNT WEAPON
                    case "WAND": // WAND
                    case "STAFF": // STAFF
                        PrimaryStat = chr.PrimaryStats.GetStrAddition() * 4.4;
                        SecondaryStat = chr.PrimaryStats.GetDexAddition();
                    break;
                    case "2HS": // TWO HANDED SWORD
                        PrimaryStat = chr.PrimaryStats.GetStrAddition() * 4.6;
                        SecondaryStat = chr.PrimaryStats.GetDexAddition();
                    break;
                    case "2HA": // TWO HANDED AXE
                    case "2HBW": // TWO HANDED BLUNT WEAPON
                        PrimaryStat = chr.PrimaryStats.GetStrAddition() * 4.8;
                        SecondaryStat = chr.PrimaryStats.GetDexAddition();
                    break;
                    case "SPEAR": // SPEAR
                    case "PA": // POLE ARM
                        PrimaryStat = chr.PrimaryStats.GetStrAddition() * 5.0;
                        SecondaryStat = chr.PrimaryStats.GetDexAddition();
                    break;
                    case "DAGGERNT": // NON-THIEF USING DAGGER
                        PrimaryStat = chr.PrimaryStats.GetStrAddition() * 4.0;
                        SecondaryStat = chr.PrimaryStats.GetDexAddition();
                    break;
                    case "DAGGERT": // THIEF USING DAGGER
                        PrimaryStat = chr.PrimaryStats.GetLukAddition() * 3.6;
                        SecondaryStat = chr.PrimaryStats.GetStrAddition() + chr.PrimaryStats.GetDexAddition();
                    break;
                    case "BOW": // BOW
                        PrimaryStat = chr.PrimaryStats.GetDexAddition() * 3.4;
                        SecondaryStat = chr.PrimaryStats.GetStrAddition();
                    break;
                    case "XBOW": // CROSSBOW
                        PrimaryStat = chr.PrimaryStats.GetDexAddition() * 3.6;
                        SecondaryStat = chr.PrimaryStats.GetStrAddition();
                    break;
                    case "CLAW": // CLAW
                        return (chr.PrimaryStats.GetLukAddition() * 1.0 + chr.PrimaryStats.GetStrAddition() + chr.PrimaryStats.GetDexAddition()) * WeaponAttack / 150.0;
                    case "NONE": // ERROR!
                    default:
                        Program.MainForm.LogAppend("Woops! Something went wrong with depicting the Weapon Type!");
                    break;
                }

                if (MeleeSkill != null) // If the player used a melee skill...
                {
                    switch (SkillID)
                    {
                        case 1001004: // POWER STRIKE
                        case 4201004: // STEAL
                            return (((PrimaryStat + SecondaryStat) * WeaponAttack / 100.0) * ElementModifier(mob, SkillID) * LevelDisadvantageModifierDmg(chr.PrimaryStats.Level, mob.Data.Level) - (mob.Data.PDD * 0.5)) * MeleeSkill.Damage;
                        case 1001005: // SLASH BLAST
                            return ((((PrimaryStat + SecondaryStat) * WeaponAttack / 100.0) * ElementModifier(mob, SkillID) * LevelDisadvantageModifierDmg(chr.PrimaryStats.Level, mob.Data.Level) - (mob.Data.PDD * 0.5)) * MeleeSkill.Damage) * Targets;
                        case 4001334: // DOUBLE STAB
                        case 4201005: // SAVAGE BLOW
                            return ((((PrimaryStat + SecondaryStat) * WeaponAttack / 100.0) * ElementModifier(mob, SkillID) * LevelDisadvantageModifierDmg(chr.PrimaryStats.Level, mob.Data.Level) - (mob.Data.PDD * 0.5)) * MeleeSkill.Damage) * MeleeSkill.HitCount;
                        default:
                            return ((PrimaryStat + SecondaryStat) * WeaponAttack / 100.0) * ElementModifier(mob, SkillID) * LevelDisadvantageModifierDmg(chr.PrimaryStats.Level, mob.Data.Level) - (mob.Data.PDD * 0.5);
                    }
                }
                else
                {
                    return ((PrimaryStat + SecondaryStat) * WeaponAttack / 100.0) * ElementModifier(mob, SkillID) * LevelDisadvantageModifierDmg(chr.PrimaryStats.Level, mob.Data.Level) - (mob.Data.PDD * 0.5);
                }
            }

            catch (Exception melee)
            {
                Program.MainForm.LogAppend("MELEE ANTI-CHEAT EXCEPTION: " + melee);
            }

            return 0.0;
        }

        // Author: wackyracer / Joren McGrew
        // This is the formula for maximum spell damage dealt by the Magician class.
        // TODO: This formula does not take the 3rd job skill "Element Amplification" into account (only because 3rd job isn't on the server yet)!
        // This formula is from: https://web.archive.org/web/20081009190310/http://www.southperry.net:80/forums/showthread.php?t=855&page=2
        // Formula Name: Japanese version (most accurate)
        // MAX = (Magic * 3.3 + Magic * Magic * 0.003365 + INT * 0.5) * Spell / 100
        // I have added elemental damage amplification/reduction to the formula.
        // I have added mob resistance calculation for damage reduction to the formula.
        // I have added mob level difference from character (if mob level is higher than character's).
        // My formula looks like...
        // ((Magic * 3.3 + Magic * Magic * 0.003365 + INT * 0.5) * Spell Attack / 100) * Element Modifier * Level Difference - (Mob Magic Defense * 0.5)
        public static double MaximumSpellDamage(Character chr, Mob mob, int SpellID)
        {
            double chrINT = chr.PrimaryStats.GetIntAddition();
            double chrMagicAttack = chr.PrimaryStats.BuffMagicAttack.N + chr.Inventory.GetTotalMAttInEquips();
            double chrMagic = Math.Min(chrINT + chrMagicAttack, 999.0);
            double SpellAttack = chr.Skills.GetSpellAttack(SpellID);

            if (chr.PrimaryStats.Job / 100 == 2 || chr.PrimaryStats.Job / 100 == 5)
            {
                return ((chrMagic * 3.3 + chrMagic * chrMagic * 0.003365 + chrINT * 0.5) * SpellAttack / 100.0) * ElementModifier(mob, SpellID) * LevelDisadvantageModifierDmg(chr.PrimaryStats.Level, mob.Data.Level) - (mob.Data.MDD * 0.5);
            }

            return 0.0; // HAAAAAAAAAAAAAAAAAAAAAAX -throws PC monitor-
        }

        // Author: wackyracer / Joren McGrew
        // This is the formula for maximum heal damage dealt by the Magician->Cleric class.
        // A special snowflake of an ability that deserves its very own calculation... F3.
        // This formula is from: https://ayumilovemaple.wordpress.com/2009/09/06/maplestory-formula-compilation/
        // Heal Damage (credit to Russt//Devil's Sunrise for Target Multiplier function):
        // MAX = (INT* 1.2 + LUK) * Magic / 1000 * Target Multiplier
        // Heal Target Multiplier: 1.5 + 5/(number of targets including yourself)
        // For reference-
        // 1 - 6.5
        // 2 - 4.0
        // 3 - 3.166
        // 4 - 2.75
        // 5 - 2.5
        // 6 - 2.333
        // Including the mob magic defense resistance calculation, elemental and level difference amplification/reduction formulas...
        // My formula looks like...
        // ((INT * 1.2 + LUK) * Magic / 1000 * (1.5 + 5 / Targets)) * Heal Damage (10% ~ 300%, based on level of Heal) * Elemental Damage Modifier * Level Difference - (Mob Magic Defense * 0.5)
        public static double MaximumHealDamage(Character chr, Mob mob, byte Targets)
        {
            if (chr.PrimaryStats.Job / 10 == 23 || chr.PrimaryStats.Job / 100 == 5)
            {
                double chrINT = chr.PrimaryStats.GetIntAddition();
                double chrLUK = chr.PrimaryStats.GetLukAddition();
                double chrMagicAttack = chr.PrimaryStats.BuffMagicAttack.N + chr.Inventory.GetTotalMAttInEquips();
                double chrMagic = Math.Min(chrINT + chrMagicAttack, 999.0);
                var chrHealData = chr.Skills.GetSkillLevelData(3000001, out byte HealLevel);
                
                if (chrHealData != null)
                {
                    return ((chrINT * 1.2 + chrLUK) * chrMagic / 1000.0 * (1.5 + 5.0 / Targets)) * (double)(chrHealData.HPProperty / 100.0) * ElementModifier(mob, 2301002) * LevelDisadvantageModifierDmg(chr.PrimaryStats.Level, mob.Data.Level) - (mob.Data.MDD * 0.5);
                }
            }

            return 0.0; // HAAAAAAAAAAAAAAAAAAAAAAX -throws PC monitor-
        }

        // Author: wackyracer / Joren McGrew
        // This is the formula for maximum damage dealt by Lucky Seven for the Rogue class.
        // Yet another special snowflake...
        // This formula is from: https://ayumilovemaple.wordpress.com/2009/09/06/maplestory-formula-compilation/
        // Lucky Seven/Triple Throw (credit to HS.net / LazyBui for recent verification):
        // MAX = (LUK* 5.0) * Weapon Attack / 100
        // Including the mob weapon defense resistance calculation, level difference amplification/reduction formulas, and critical strike formula...
        // My formula looks like...
        // ((LUK * 5.0) * Weapon Attack / 100) * Lucky Seven Damage (55% ~ 150%, based on level of Lucky Seven) * Level Difference - (Mob Weapon Defense * 0.5)
        public static double MaximumLuckySevenDamage(Character chr, Mob mob, int ClientTotalDamage)
        {
            if (chr.PrimaryStats.Job / 100 == 4 || chr.PrimaryStats.Job / 100 == 5)
            {
                double chrLUK = chr.PrimaryStats.GetLukAddition();
                double chrWeaponAttack = chr.PrimaryStats.BuffWeaponAttack.N + chr.Inventory.GetTotalWAttackInEquips(true);
                var chrLuckySevenData = chr.Skills.GetSkillLevelData(4001344, out byte LuckySevenLevel);

                if (chrLuckySevenData != null)
                {
                    double MaxDamageWithoutCrit = (((chrLUK * 5.0) * chrWeaponAttack / 100.0) * (double)(chrLuckySevenData.Damage / 100.0) * LevelDisadvantageModifierDmg(chr.PrimaryStats.Level, mob.Data.Level) - (mob.Data.PDD * 0.5)) * chrLuckySevenData.BulletUsage;
                    double MaxDamageWithCrit = (((chrLUK * 5.0) * chrWeaponAttack / 100.0) * (double)(chrLuckySevenData.Damage / 100.0) * LevelDisadvantageModifierDmg(chr.PrimaryStats.Level, mob.Data.Level) - (mob.Data.PDD * 0.5) * CriticalStrikeModifier(chr)) * chrLuckySevenData.BulletUsage;

                    if (chr.PrimaryStats.Job / 10 == 41 || chr.PrimaryStats.Job / 100 == 5)
                    {
                        if (ClientTotalDamage <= MaxDamageWithoutCrit)
                        {
                            return MaxDamageWithoutCrit;
                        }

                        else
                        {
                            return MaxDamageWithCrit;
                        }
                    }

                    return MaxDamageWithoutCrit;
                }
            }

            return 0.0; // HAAAAAAAAAAAAAAAAAAAAAAX -throws PC monitor-
        }

        // Author: wackyracer / Joren McGrew
        // This is the formula for maximum damage dealt by Power Knockback for the Hunter and Crossbowman class.
        // Yet another special snowflake...
        // This formula is from: https://ayumilovemaple.wordpress.com/2009/09/06/maplestory-formula-compilation/
        // Power Knock Back (both weapons, credit to AGF/Fiel):
        // MAX = (DEX* 3.4 + STR) * Weapon Attack / 150
        // Including the mob weapon defense resistance calculation, level difference amplification/reduction formulas, and critical strike formula...
        // My formula looks like...
        // ((DEX * 3.4 + STR) * Weapon Attack / 150) * (105% ~ 200%, based on level of Power Knockback) * Level Difference - (Mob Weapon Defense * 0.5)
        public static double MaximumPowerKnockbackDamage(Character chr, Mob mob, int ClientTotalDamage)
        {
            if (chr.PrimaryStats.Job / 10 == 31 || chr.PrimaryStats.Job / 10 == 32 || chr.PrimaryStats.Job / 100 == 5)
            {
                double chrSTR = chr.PrimaryStats.GetStrAddition();
                double chrDEX = chr.PrimaryStats.GetDexAddition();
                double chrWeaponAttack = chr.PrimaryStats.BuffWeaponAttack.N + chr.Inventory.GetTotalWAttackInEquips(false);
                var chrPowerKnockbackData = chr.PrimaryStats.Job == 310 ? chr.Skills.GetSkillLevelData(3101003, out byte PowerKnockbackLevel1) : chr.Skills.GetSkillLevelData(3201003, out byte PowerKnockbackLevel2);

                if (chrPowerKnockbackData != null)
                {
                    double MaxDamageWithoutCrit = ((chrDEX * 3.4 + chrSTR) * chrWeaponAttack / 150.0) * (double)(chrPowerKnockbackData.Damage / 100.0) * LevelDisadvantageModifierDmg(chr.PrimaryStats.Level, mob.Data.Level) - (mob.Data.PDD * 0.5);
                    double MaxDamageWithCrit = ((chrDEX * 3.4 + chrSTR) * chrWeaponAttack / 150.0) * (double)(chrPowerKnockbackData.Damage / 100.0) * LevelDisadvantageModifierDmg(chr.PrimaryStats.Level, mob.Data.Level) - (mob.Data.PDD * 0.5) * CriticalStrikeModifier(chr);

                    // Unsure if it can crit or not, but I'll put the code here just in-case.
                    if (ClientTotalDamage <= MaxDamageWithoutCrit)
                    {
                        return MaxDamageWithoutCrit;
                    }

                    else
                    {
                        return MaxDamageWithCrit;
                    }
                }
            }

            return 0.0; // HAAAAAAAAAAAAAAAAAAAAAAX -throws PC monitor-
        }

        // Author: wackyracer / Joren McGrew
        // This is the formula for maximum damage dealt by Arrow Bomb for the Hunter class.
        // Arrow Bomb:
        // Impact Hit = 50% damage * Critical Multiplier
        // Splash Hits = Skill damage * Critical Multiplier <-- This is the one I'll be using for max-hit detection - wackyracer
        // (Arrow Bomb's critical replaces the standard critical bonus, which is normally inserted into the skill damage %, as shown in Order of Operations above. It still uses the same value for critical, however.)
        // NEEDS TESTING!!!
        // Including the mob weapon defense resistance calculation, level difference amplification/reduction formulas, and critical strike formula...
        // My formula looks like: ((DEX * 3.4 + STR) * Weapon Attack / 100) * (Bomb Arrow Damage Modifier * Critical Strike Modifier) * Level Disadvantage - (Mob Weapon Defense * 0.5)
        public static double MaximumArrowBombDamage(Character chr, Mob mob, int ArrowID, int ClientTotalDamage)
        {
            try
            {
                // If they're a Bowman or GM...
                if (chr.PrimaryStats.Job / 10 == 31 || chr.PrimaryStats.Job / 100 == 5)
                {
                    // Bow
                    // Primary: DEX * 3.4
                    // Secondary: STR
                    if (ArrowID / 1000 == 2060)
                    {
                        double chrSTR = chr.PrimaryStats.GetStrAddition();
                        double chrDEX = chr.PrimaryStats.GetDexAddition();
                        double chrWeaponAttack = chr.PrimaryStats.BuffWeaponAttack.N + chr.Inventory.GetTotalWAttackInEquips(true);
                        var chrArrowBombData = chr.Skills.GetSkillLevelData(3101005, out byte ArrowBombLevel);

                        if (chrArrowBombData != null)
                        {
                            double MaxDamageWithoutCrit = ((chrDEX * 3.4 + chrSTR) * chrWeaponAttack / 100.0) * ((chrArrowBombData.Damage / 100.0) * CriticalStrikeModifier(chr)) * LevelDisadvantageModifierDmg(chr.PrimaryStats.Level, mob.Data.Level) - (mob.Data.PDD * 0.5);
                            double MaxDamageWithCrit = ((chrDEX * 3.4 + chrSTR) * chrWeaponAttack / 100.0) * ((chrArrowBombData.Damage / 100.0) * CriticalStrikeModifier(chr)) * LevelDisadvantageModifierDmg(chr.PrimaryStats.Level, mob.Data.Level) - (mob.Data.PDD * 0.5) * CriticalStrikeModifier(chr);

                            if (ClientTotalDamage <= MaxDamageWithoutCrit)
                            {
                                return MaxDamageWithoutCrit;
                            }

                            else
                            {
                                return MaxDamageWithCrit;
                            }
                        }
                    }
                }
            }
            
            catch (Exception bomb)
            {
                Program.MainForm.LogAppend("DAMAGE FORMULA ARROWBOMB EXCEPTION: " + bomb);
                return 0.0;
            }

            return 0.0; // HAAAAAAAAAAAAAAAAAAAAAAX -throws PC monitor-
        }

        // Author: wackyracer / Joren McGrew
        // This is the formula for maximum damage dealt by regular ranged attacks.
        // Each class has a different formula for their ranged attacks.
        // The General Formula is: MAX = (Primary Stat + Secondary Stat) * Weapon Attack / 100
        // Including the mob weapon defense resistance calculation, level difference amplification/reduction formulas, and critical strike formula...
        // Something seems off here. I can smell it. I think Bow/Crossbow Mastery needs to be taken into account but I can't find the formula for it online...
        // Guess I can only find out with testing... F3.
        public static double MaximumRangedDamage(Character chr, Mob mob, int SkillID, int StarID, byte Targets, int ClientTotalDamage)
        {
            // If they're a Bowman, Thief, or GM...
            if (chr.PrimaryStats.Job / 100 == 3 || chr.PrimaryStats.Job / 100 == 4 || chr.PrimaryStats.Job / 100 == 5)
            {
                // Bow
                // Primary: DEX * 3.4
                // Secondary: STR
                if (StarID / 1000 == 2060)
                {
                    double chrSTR = chr.PrimaryStats.GetStrAddition();
                    double chrDEX = chr.PrimaryStats.GetDexAddition();
                    double chrWeaponAttack = chr.PrimaryStats.BuffWeaponAttack.N + chr.Inventory.GetTotalWAttackInEquips(true);
                    double MaxDamageWithoutCrit = ((chrDEX * 3.4 + chrSTR) * chrWeaponAttack / 100.0) * LevelDisadvantageModifierDmg(chr.PrimaryStats.Level, mob.Data.Level) - (mob.Data.PDD * 0.5);
                    double MaxDamageWithCrit = ((chrDEX * 3.4 + chrSTR) * chrWeaponAttack / 100.0) * LevelDisadvantageModifierDmg(chr.PrimaryStats.Level, mob.Data.Level) - (mob.Data.PDD * 0.5) * CriticalStrikeModifier(chr);

                    if (ClientTotalDamage <= MaxDamageWithoutCrit)
                    {
                        switch (SkillID) // NEEDS TESTING
                        {
                            case 3001004: // Arrow Blow
                                var chrArrowBlowData = chr.Skills.GetSkillLevelData(3001004, out byte ArrowBlowLevel);
                                return MaxDamageWithoutCrit * (chrArrowBlowData.Damage / 100.0);
                            case 3001005: // Double Shot
                                var chrDoubleShotData = chr.Skills.GetSkillLevelData(3001005, out byte DoubleShotLevel);
                                return (MaxDamageWithoutCrit * (chrDoubleShotData.Damage / 100.0)) * 2.0;
                            case 3100001: // Final Attack
                                return (MaxDamageWithoutCrit * (FinalAttackModifier(chr, SkillID)));
                            default:
                                return MaxDamageWithoutCrit;
                        }
                    }

                    else
                    {
                        switch (SkillID) // NEEDS TESTING
                        {
                            case 3001004: // Arrow Blow
                                var chrArrowBlowData = chr.Skills.GetSkillLevelData(3001004, out byte ArrowBlowLevel);
                                return MaxDamageWithCrit * (chrArrowBlowData.Damage / 100.0);
                            case 3001005: // Double Shot
                                var chrDoubleShotData = chr.Skills.GetSkillLevelData(3001005, out byte DoubleShotLevel);
                                return (MaxDamageWithCrit * (chrDoubleShotData.Damage / 100.0)) * 2.0;
                            case 3100001: // Final Attack
                                return (MaxDamageWithCrit * (FinalAttackModifier(chr, SkillID)));
                            default:
                                return MaxDamageWithCrit;
                        }
                    }
                }

                // Crossbow
                // Primary: DEX * 3.6
                // Secondary: STR
                else if (StarID / 1000 == 2061)
                {
                    double chrSTR = chr.PrimaryStats.GetStrAddition();
                    double chrDEX = chr.PrimaryStats.GetDexAddition();
                    double chrWeaponAttack = chr.PrimaryStats.BuffWeaponAttack.N + chr.Inventory.GetTotalWAttackInEquips(true);
                    double MaxDamageWithoutCrit = ((chrDEX * 3.6 + chrSTR) * chrWeaponAttack / 100.0) * LevelDisadvantageModifierDmg(chr.PrimaryStats.Level, mob.Data.Level) - (mob.Data.PDD * 0.5);
                    double MaxDamageWithCrit = ((chrDEX * 3.6 + chrSTR) * chrWeaponAttack / 100.0) * LevelDisadvantageModifierDmg(chr.PrimaryStats.Level, mob.Data.Level) - (mob.Data.PDD * 0.5) * CriticalStrikeModifier(chr);

                    if (ClientTotalDamage <= MaxDamageWithoutCrit)
                    {
                        switch (SkillID) // NEEDS TESTING
                        {
                            case 3001004: // Arrow Blow
                                var chrArrowBlowData = chr.Skills.GetSkillLevelData(3001004, out byte ArrowBlowLevel);
                                return MaxDamageWithoutCrit * (chrArrowBlowData.Damage / 100.0);
                            case 3001005: // Double Shot
                                var chrDoubleShotData = chr.Skills.GetSkillLevelData(3001005, out byte DoubleShotLevel);
                                return (MaxDamageWithoutCrit * (chrDoubleShotData.Damage / 100.0)) * 2.0;
                            case 3200001: // Final Attack
                                return (MaxDamageWithoutCrit * (FinalAttackModifier(chr, SkillID)));
                            case 3201005: // Iron Arrow (needs testing)
                                //var chrIronArrowData = chr.Skills.GetSkillLevelData(3201005, out byte IronArrowLevel);
                                return (MaxDamageWithoutCrit * (10.0 * (1.0 - Math.Pow(0.9, Targets))));
                            default:
                                return MaxDamageWithoutCrit;
                        }
                    }

                    else
                    {
                        switch (SkillID) // NEEDS TESTING
                        {
                            case 3001004: // Arrow Blow
                                var chrArrowBlowData = chr.Skills.GetSkillLevelData(3001004, out byte ArrowBlowLevel);
                                return MaxDamageWithCrit * chrArrowBlowData.Damage;
                            case 3001005: // Double Shot
                                var chrDoubleShotData = chr.Skills.GetSkillLevelData(3001005, out byte DoubleShotLevel);
                                return (MaxDamageWithCrit * chrDoubleShotData.Damage) * 2.0;
                            case 3200001: // Final Attack
                                return (MaxDamageWithCrit * (FinalAttackModifier(chr, SkillID)));
                            case 3201005: // Iron Arrow (needs testing)
                                //var chrIronArrowData = chr.Skills.GetSkillLevelData(3201005, out byte IronArrowLevel);
                                return (MaxDamageWithCrit * (10.0 * (1.0 - Math.Pow(0.9, Targets))));
                            default:
                                return MaxDamageWithCrit;
                        }
                    }
                }

                // Throwing Stars (Thieves)
                // Primary: LUK * 3.6
                // Secondary: STR + DEX
                else if (StarID / 10000 == 207)
                {
                    double chrSTR = chr.PrimaryStats.GetStrAddition();
                    double chrDEX = chr.PrimaryStats.GetDexAddition();
                    double chrLUK = chr.PrimaryStats.GetLukAddition();
                    double chrWeaponAttack = chr.PrimaryStats.BuffWeaponAttack.N + chr.Inventory.GetTotalWAttackInEquips(true);
                    double MaxDamageWithoutCrit = ((chrLUK * 3.6 + (chrSTR + chrDEX)) * chrWeaponAttack / 100.0) * LevelDisadvantageModifierDmg(chr.PrimaryStats.Level, mob.Data.Level) - (mob.Data.PDD * 0.5);
                    double MaxDamageWithCrit = ((chrLUK * 3.6 + (chrSTR + chrDEX)) * chrWeaponAttack / 100.0) * LevelDisadvantageModifierDmg(chr.PrimaryStats.Level, mob.Data.Level) - (mob.Data.PDD * 0.5) * CriticalStrikeModifier(chr);

                    if (chr.PrimaryStats.Job / 10 == 41 || chr.PrimaryStats.Job / 100 == 5)
                    {
                        if (ClientTotalDamage <= MaxDamageWithoutCrit)
                        { // NEEDS TESTING
                            if (SkillID == 4101005) // Drain
                            {
                                var chrDrainData = chr.Skills.GetSkillLevelData(4101005, out byte DrainLevel);
                                return (MaxDamageWithoutCrit * (chrDrainData.Damage / 100.0));
                            }
                            else
                            {
                                return MaxDamageWithoutCrit;
                            }
                        }

                        else
                        {
                            if (SkillID == 4101005) // Drain
                            {
                                var chrDrainData = chr.Skills.GetSkillLevelData(4101005, out byte DrainLevel);
                                return (MaxDamageWithCrit * (chrDrainData.Damage / 100.0));
                            }
                            else
                            {
                                return MaxDamageWithCrit;
                            }
                        }
                    }

                    else
                    {
                        return MaxDamageWithoutCrit;
                    }
                }

                else
                {
                    return 0.0; // HAAAAAAAAAAAAAAAAAAAAAAX -throws PC monitor-
                }
            }

            else
            {
                return 0.0; // HAAAAAAAAAAAAAAAAAAAAAAX -throws PC monitor-
            }
        }
    }
}