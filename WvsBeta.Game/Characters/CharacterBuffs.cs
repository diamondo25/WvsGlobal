using System.Diagnostics;
using WvsBeta.Common;

namespace WvsBeta.Game
{
    public class CharacterBuffs
    {
        public Character Character { get; set; }
        public byte mComboCount { get; set; }

        public CharacterBuffs(Character chr)
        {
            Character = chr;
        }

        public bool HasGMHide()
        {
            return Character.PrimaryStats.HasBuff(Constants.Gm.Skills.Hide);
        }

        public void AddItemBuff(int itemid)
        {
            var data = DataProvider.Items[itemid];
            long buffTime = data.BuffTime;

            var expireTime = BuffStat.GetTimeForBuff(buffTime);
            var ps = Character.PrimaryStats;
            var value = -itemid;
            BuffValueTypes added = 0;

            if (data.Accuracy > 0)
                added |= ps.BuffAccurancy.Set(value, data.Accuracy, expireTime);

            if (data.Avoidance > 0)
                added |= ps.BuffAvoidability.Set(value, data.Avoidance, expireTime);

            if (data.Speed > 0)
                added |= ps.BuffSpeed.Set(value, data.Speed, expireTime);

            if (data.MagicAttack > 0)
                added |= ps.BuffMagicAttack.Set(value, data.MagicAttack, expireTime);

            if (data.WeaponAttack > 0)
                added |= ps.BuffWeaponAttack.Set(value, data.WeaponAttack, expireTime);

            if (data.WeaponDefense > 0)
                added |= ps.BuffWeaponDefense.Set(value, data.WeaponDefense, expireTime);

            if (data.Thaw > 0)
                added |= ps.BuffThaw.Set(value, data.Thaw, expireTime);

            if (added != 0)
            {
                FinalizeBuff(added, 0);
            }

            BuffValueTypes removed = 0;

            if (data.Cures.HasFlag(ItemData.CureFlags.Weakness))
                removed |= ps.BuffWeakness.Reset();

            if (data.Cures.HasFlag(ItemData.CureFlags.Poison))
                removed |= ps.BuffPoison.Reset();

            if (data.Cures.HasFlag(ItemData.CureFlags.Curse))
                removed |= ps.BuffCurse.Reset();

            if (data.Cures.HasFlag(ItemData.CureFlags.Darkness))
                removed |= ps.BuffDarkness.Reset();

            if (data.Cures.HasFlag(ItemData.CureFlags.Seal))
                removed |= ps.BuffSeal.Reset();

            FinalizeDebuff(removed);
        }

        public void Dispell()
        {
            var ps = Character.PrimaryStats;
            BuffValueTypes removed = 0;

            removed |= ps.BuffWeakness.Reset();
            removed |= ps.BuffPoison.Reset();
            removed |= ps.BuffCurse.Reset();
            removed |= ps.BuffDarkness.Reset();
            removed |= ps.BuffSeal.Reset();
            removed |= ps.BuffStun.Reset();

            FinalizeDebuff(removed);
        }

        public void CancelHyperBody()
        {
            var primaryStats = Character.PrimaryStats;
            primaryStats.BuffBonuses.MaxHP = 0;
            primaryStats.BuffBonuses.MaxMP = 0;


            if (primaryStats.HP > primaryStats.GetMaxHP(false))
            {
                Character.ModifyHP(primaryStats.GetMaxHP(false));
            }

            if (primaryStats.MP > primaryStats.GetMaxMP(false))
            {
                Character.ModifyMP(primaryStats.GetMaxMP(false));
            }

            //mCharacter.SetMaxHP(primaryStats.GetMaxHP(false));
            //mCharacter.SetMaxMP(primaryStats.GetMaxMP(false));
        }


        public void AddBuff(int SkillID, byte level, short delay = 0)
        {
            if (!BuffDataProvider.SkillBuffValues.TryGetValue(SkillID, out var flags))
            {
                return;
            }

            
            if (level == 0xFF)
            {
                level = Character.Skills.Skills[SkillID];
            }
            var data = DataProvider.Skills[SkillID].Levels[level];


            long time = data.BuffTime * 1000;
            time += delay;

            // Fix for MesoGuard expiring... hurr
            if (SkillID == Constants.ChiefBandit.Skills.MesoGuard)
                time += 1000 * 1000;
            Trace.WriteLine($"Adding buff from skill {SkillID} lvl {level}: {time}. Flags {flags}");

            var expireTime = BuffStat.GetTimeForBuff(time);
            var ps = Character.PrimaryStats;
            BuffValueTypes added = 0;

            if (flags.HasFlag(BuffValueTypes.WeaponAttack)) added |= ps.BuffWeaponAttack.Set(SkillID, data.WeaponAttack, expireTime);
            if (flags.HasFlag(BuffValueTypes.WeaponDefense)) added |= ps.BuffWeaponDefense.Set(SkillID, data.WeaponDefense, expireTime);
            if (flags.HasFlag(BuffValueTypes.MagicAttack)) added |= ps.BuffMagicAttack.Set(SkillID, data.MagicAttack, expireTime);
            if (flags.HasFlag(BuffValueTypes.MagicDefense)) added |= ps.BuffMagicDefense.Set(SkillID, data.MagicDefense, expireTime);
            if (flags.HasFlag(BuffValueTypes.Accurancy)) added |= ps.BuffAccurancy.Set(SkillID, data.Accurancy, expireTime);
            if (flags.HasFlag(BuffValueTypes.Avoidability)) added |= ps.BuffAvoidability.Set(SkillID, data.Avoidability, expireTime);
            //if (flags.Contains(BuffValueTypes.Hands)) added |= ps.BuffHands.Set(SkillID, data.Hands, expireTime);
            if (flags.HasFlag(BuffValueTypes.Speed)) added |= ps.BuffSpeed.Set(SkillID, data.Speed, expireTime);
            if (flags.HasFlag(BuffValueTypes.Jump)) added |= ps.BuffJump.Set(SkillID, data.Jump, expireTime);
            if (flags.HasFlag(BuffValueTypes.MagicGuard)) added |= ps.BuffMagicGuard.Set(SkillID, data.XValue, expireTime);
            if (flags.HasFlag(BuffValueTypes.DarkSight)) added |= ps.BuffDarkSight.Set(SkillID, data.XValue, expireTime);
            if (flags.HasFlag(BuffValueTypes.Booster)) added |= ps.BuffBooster.Set(SkillID, data.XValue, expireTime);
            if (flags.HasFlag(BuffValueTypes.PowerGuard)) added |= ps.BuffPowerGuard.Set(SkillID, data.XValue, expireTime);
            if (flags.HasFlag(BuffValueTypes.MaxHP)) added |= ps.BuffMaxHP.Set(SkillID, data.XValue, expireTime);
            if (flags.HasFlag(BuffValueTypes.MaxMP)) added |= ps.BuffMaxMP.Set(SkillID, data.YValue, expireTime);
            if (flags.HasFlag(BuffValueTypes.Invincible)) added |= ps.BuffInvincible.Set(SkillID, data.XValue, expireTime);
            if (flags.HasFlag(BuffValueTypes.SoulArrow)) added |= ps.BuffSoulArrow.Set(SkillID, data.XValue, expireTime);
            if (flags.HasFlag(BuffValueTypes.ComboAttack)) added |= ps.BuffComboAttack.Set(SkillID, data.XValue, expireTime);
            if (flags.HasFlag(BuffValueTypes.Charges)) added |= ps.BuffCharges.Set(SkillID, data.XValue, expireTime);
            if (flags.HasFlag(BuffValueTypes.DragonBlood)) added |= ps.BuffDragonBlood.Set(SkillID, data.XValue, expireTime);
            if (flags.HasFlag(BuffValueTypes.HolySymbol)) added |= ps.BuffHolySymbol.Set(SkillID, data.XValue, expireTime);
            if (flags.HasFlag(BuffValueTypes.MesoUP)) added |= ps.BuffMesoUP.Set(SkillID, data.XValue, expireTime);
            if (flags.HasFlag(BuffValueTypes.ShadowPartner)) added |= ps.BuffShadowPartner.Set(SkillID, data.XValue, expireTime);
            if (flags.HasFlag(BuffValueTypes.PickPocketMesoUP)) added |= ps.BuffPickPocketMesoUP.Set(SkillID, data.XValue, expireTime);
            if (flags.HasFlag(BuffValueTypes.MesoGuard))
            {
                added |= ps.BuffMesoGuard.Set(SkillID, data.XValue, expireTime);
                ps.BuffMesoGuard.MesosLeft = data.MesosUsage;
            }

            FinalizeBuff(added, delay);
        }

        public void FinalizeBuff(BuffValueTypes added, short delay, bool sendPacket = true)
        {
            if (added == 0) return;
            Trace.WriteLine($"Added buffs {added}");

            Character.FlushDamageLog();

            if (!sendPacket) return;
            BuffPacket.SetTempStats(Character, added, delay);
            MapPacket.SendPlayerBuffed(Character, added, delay);
        }

        public void FinalizeDebuff(BuffValueTypes removed, bool sendPacket = true)
        {
            if (removed == 0) return;
            Trace.WriteLine($"Removed buffs {removed}");

            Character.FlushDamageLog();

            if (!sendPacket) return;
            BuffPacket.ResetTempStats(Character, removed);
            MapPacket.SendPlayerDebuffed(Character, removed);
        }
        
    }
}