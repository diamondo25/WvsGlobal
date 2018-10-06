using System;
using System.Diagnostics;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;
using WvsBeta.Common.Tracking;

namespace WvsBeta.Game
{
    public class DamageReflectorSkillData
    {
        public byte Reduction = 0;
        public int Damage = 0;
        public int ObjectID = 0;
        public bool IsPhysical = false;
        public Pos Position = new Pos(0, 0);
    }

    public static class CharacterStatsPacket
    {
        [Flags]
        public enum StatFlags : uint
        {
            Skin = 0x01,
            Eyes = 0x02,
            Hair = 0x04,
            Pet = 0x08,
            Level = 0x10,
            Job = 0x20,
            Str = 0x40,
            Dex = 0x80,
            Int = 0x100,
            Luk = 0x200,
            Hp = 0x400,
            MaxHp = 0x800,
            Mp = 0x1000,
            MaxMp = 0x2000,
            Ap = 0x4000,
            Sp = 0x8000,
            Exp = 0x10000,
            Fame = 0x20000,
            Mesos = 0x40000
        };

        public static void HandleStats(Character chr, Packet packet)
        {
            uint flag = packet.ReadUInt();
            if (chr.AssertForHack(chr.PrimaryStats.AP <= 0, "Trying to use AP, but nothing left."))
            {
                InventoryPacket.NoChange(chr);
                return;
            }

            short jobTrack = Constants.getJobTrack(chr.PrimaryStats.Job);

            switch ((StatFlags)flag)
            {
                case StatFlags.Str:
                    {
                        if (chr.PrimaryStats.Str >= Constants.MaxStat)
                        {
                            InventoryPacket.NoChange(chr);
                            return;
                        }
                        chr.AddStr(1);
                        break;
                    }
                case StatFlags.Dex:
                    {
                        if (chr.PrimaryStats.Dex >= Constants.MaxStat)
                        {
                            InventoryPacket.NoChange(chr);
                            return;
                        }
                        chr.AddDex(1);
                        break;
                    }
                case StatFlags.Int:
                    {
                        if (chr.PrimaryStats.Int >= Constants.MaxStat)
                        {
                            InventoryPacket.NoChange(chr);
                            return;
                        }
                        chr.AddInt(1);
                        break;
                    }
                case StatFlags.Luk:
                    {
                        if (chr.PrimaryStats.Luk >= Constants.MaxStat)
                        {
                            InventoryPacket.NoChange(chr);
                            return;
                        }
                        chr.AddLuk(1);
                        break;
                    }
                case StatFlags.MaxHp:
                    {
                        if (chr.PrimaryStats.MaxHP >= Constants.MaxMaxHp)
                        {
                            InventoryPacket.NoChange(chr);
                            return;
                        }
                        short hpGain = 0;

                        hpGain += RNG.Range.generate(
                            Constants.HpMpFormulaArguments[jobTrack, 1, (int)Constants.HpMpFormulaFields.HPMin],
                            Constants.HpMpFormulaArguments[jobTrack, 1, (int)Constants.HpMpFormulaFields.HPMax],
                            true
                        );

                        byte improvedMaxHpIncreaseLvl = chr.Skills.GetSkillLevel(Constants.Swordsman.Skills.ImprovedMaxHpIncrease);
                        if (improvedMaxHpIncreaseLvl > 0)
                        {
                            hpGain += CharacterSkills.GetSkillLevelData(Constants.Swordsman.Skills.ImprovedMaxHpIncrease, improvedMaxHpIncreaseLvl).XValue;
                        }

                        chr.ModifyMaxHP(hpGain);
                        break;
                    }
                case StatFlags.MaxMp:
                    {
                        if (chr.PrimaryStats.MaxMP >= Constants.MaxMaxMp)
                        {
                            InventoryPacket.NoChange(chr);
                            return;
                        }
                        short mpGain = 0;
                        short intt = chr.PrimaryStats.GetIntAddition(true);

                        mpGain += RNG.Range.generate(
                            Constants.HpMpFormulaArguments[jobTrack, 1, (int)Constants.HpMpFormulaFields.MPMin],
                            Constants.HpMpFormulaArguments[jobTrack, 1, (int)Constants.HpMpFormulaFields.MPMax],
                            true
                        );

                        // Additional buffing through INT stats
                        mpGain += (short)(
                            intt *
                            Constants.HpMpFormulaArguments[jobTrack, 1, (int)Constants.HpMpFormulaFields.MPIntStatMultiplier] /
                            200
                        );

                        byte improvedMaxMpIncreaseLvl = chr.Skills.GetSkillLevel(Constants.Magician.Skills.ImprovedMaxMpIncrease);
                        if (improvedMaxMpIncreaseLvl > 0)
                        {
                            mpGain += CharacterSkills.GetSkillLevelData(Constants.Magician.Skills.ImprovedMaxMpIncrease, improvedMaxMpIncreaseLvl).XValue;
                        }

                        chr.ModifyMaxMP(mpGain);
                        break;
                    }
                default:
                    {
                        Program.MainForm.LogAppend("Unknown type {0:X4}", flag);
                        break;
                    }
            }

            chr.AddAP(-1, true);
            chr.PrimaryStats.CalculateAdditions(false, false);
        }

        public static void HandleHeal(Character chr, Packet packet)
        {
            // 2B 00 14 00 00 00 00 03 00 00
            int flag = packet.ReadInt();


            short hp = (flag & 0x0400) != 0 ? packet.ReadShort() : (short)0;
            short mp = (flag & 0x1000) != 0 ? packet.ReadShort() : (short)0;

            byte extraHealEffect = packet.ReadByte();

            if (chr.PrimaryStats.HP == 0) return;


            if (hp > 400 ||
                mp > 1000 ||
                (hp > 0 && mp > 0))
            {
                return;
            }

            if (hp > 0)
            {
                // Check endure and stuff here...
                chr.ModifyHP(hp);
                /**
                if (chr.mParty != null)
                {
                    chr.mParty.UpdatePartyMemberHP(chr);
                    chr.mParty.ReceivePartyMemberHP(chr);
                }
                 * **/
            }

            if (mp > 0)
            {
                chr.ModifyMP(mp);
            }
        }

        public static void SendStatChange(Character chr, uint flag, byte value, bool isBySelf = false)
        {
            Packet pw = new Packet(ServerMessages.STAT_CHANGED);
            pw.WriteBool(isBySelf);
            pw.WriteUInt(flag);
            pw.WriteByte(value);
            chr.SendPacket(pw);
        }

        public static void SendStatChange(Character chr, uint flag, short value, bool isBySelf = false)
        {
            Packet pw = new Packet(ServerMessages.STAT_CHANGED);
            pw.WriteBool(isBySelf);
            pw.WriteUInt(flag);
            pw.WriteShort(value);
            chr.SendPacket(pw);
        }

        public static void SendStatChange(Character chr, uint flag, int value, bool isBySelf = false)
        {
            Packet pw = new Packet(ServerMessages.STAT_CHANGED);
            pw.WriteBool(isBySelf);
            pw.WriteUInt(flag);
            pw.WriteInt(value);
            chr.SendPacket(pw);
        }

        public static void SendStatChange(Character chr, uint flag, long value, bool isBySelf = false)
        {
            Packet pw = new Packet(ServerMessages.STAT_CHANGED);
            pw.WriteBool(isBySelf);
            pw.WriteUInt(flag);
            pw.WriteLong(value);
            chr.SendPacket(pw);
        }

        public static void SendUpdateStat(Character chr, bool ExcelRequest, StatFlags StatFlag)
        {
            if (ExcelRequest || StatFlag > 0)
            {
                Packet pw = new Packet(ServerMessages.STAT_CHANGED);
                pw.WriteBool(ExcelRequest);
                pw.WriteUInt((uint)StatFlag);

                if ((StatFlag & StatFlags.Skin) == StatFlags.Skin)
                    pw.WriteByte(chr.Skin);
                if ((StatFlag & StatFlags.Eyes) == StatFlags.Eyes)
                    pw.WriteInt(chr.Face);
                if ((StatFlag & StatFlags.Hair) == StatFlags.Hair)
                    pw.WriteInt(chr.Hair);

                if ((StatFlag & StatFlags.Pet) == StatFlags.Pet)
                    pw.WriteLong(chr.PetCashId);

                if ((StatFlag & StatFlags.Level) == StatFlags.Level)
                    pw.WriteByte(chr.Level);
                if ((StatFlag & StatFlags.Job) == StatFlags.Job)
                    pw.WriteShort(chr.PrimaryStats.Job);
                if ((StatFlag & StatFlags.Str) == StatFlags.Str)
                    pw.WriteShort(chr.PrimaryStats.Str);
                if ((StatFlag & StatFlags.Dex) == StatFlags.Dex)
                    pw.WriteShort(chr.PrimaryStats.Dex);
                if ((StatFlag & StatFlags.Int) == StatFlags.Int)
                    pw.WriteShort(chr.PrimaryStats.Int);
                if ((StatFlag & StatFlags.Luk) == StatFlags.Luk)
                    pw.WriteShort(chr.PrimaryStats.Luk);

                if ((StatFlag & StatFlags.Hp) == StatFlags.Hp)
                    pw.WriteShort(chr.PrimaryStats.HP);
                if ((StatFlag & StatFlags.MaxHp) == StatFlags.MaxHp)
                    pw.WriteShort(chr.PrimaryStats.MaxHP);
                if ((StatFlag & StatFlags.Mp) == StatFlags.Mp)
                    pw.WriteShort(chr.PrimaryStats.MP);
                if ((StatFlag & StatFlags.MaxMp) == StatFlags.MaxMp)
                    pw.WriteShort(chr.PrimaryStats.MaxMP);

                if ((StatFlag & StatFlags.Ap) == StatFlags.Ap)
                    pw.WriteShort(chr.PrimaryStats.AP);
                if ((StatFlag & StatFlags.Sp) == StatFlags.Sp)
                    pw.WriteShort(chr.PrimaryStats.SP);

                if ((StatFlag & StatFlags.Exp) == StatFlags.Exp)
                    pw.WriteInt(chr.PrimaryStats.EXP);

                if ((StatFlag & StatFlags.Fame) == StatFlags.Fame)
                    pw.WriteShort(chr.PrimaryStats.Fame);

                if ((StatFlag & StatFlags.Mesos) == StatFlags.Mesos)
                    pw.WriteInt(chr.Inventory.Mesos);

                if ((StatFlag & StatFlags.Pet) == StatFlags.Pet)
                    pw.WriteBool(false);

                chr.SendPacket(pw);
            }
        }

        public static void HandleCharacterDamage(Character chr, Packet pr)
        {
            //1A FF 03 00 00 00 00 00 00 00 00 04 87 01 00 00 00
            sbyte attack = pr.ReadSByte();
            int damage = pr.ReadInt();
            int reducedDamage = damage;
            int actualHPEffect = -damage;
            int actualMPEffect = 0;
            int healSkillId = 0;
            Mob mob = null;

            if (chr.AssertForHack(damage < -1, "Less than -1 (" + damage + ") damage in HandleCharacterDamage"))
            {
                return;
            }

            if (chr.PrimaryStats.HP == 0) return;

            byte mobSkillId = 0, mobSkillLevel = 0;

            if (attack <= -2)
            {
                mobSkillLevel = pr.ReadByte();
                mobSkillId = pr.ReadByte(); // (short >> 8)

                Trace.WriteLine($"Got a hit with {attack} attack, mobSkillLevel {mobSkillLevel}, mobSkillId {mobSkillId}");

            }
            else
            {
                int magicAttackElement = 0;
                if (pr.ReadBool())
                {
                    magicAttackElement = pr.ReadInt();
                    // 0 = no element (Grendel the Really Old, 9001001)
                    // 1 = Ice (Celion? blue, 5120003)
                    // 2 = Lightning (Regular big Sentinel, 3000000)
                    // 3 = Fire (Fire sentinel, 5200002)
                }

                var mobMapId = pr.ReadInt();
                var mobId = pr.ReadInt();

                mob = chr.Field.GetMob(mobMapId);
                if (mob == null ||
                    mobId != mob.MobID)
                {
                    return;
                }

                // Newer ver: int nCalcDamageMobStatIndex
                var stance = pr.ReadByte();
                var isReflected = pr.ReadBool();

                byte reflectHitAction = 0;
                short reflectX = 0, reflectY = 0;
                if (isReflected)
                {
                    reflectHitAction = pr.ReadByte();
                    reflectX = pr.ReadShort();
                    reflectY = pr.ReadShort();
                }

                if (chr.PrimaryStats.BuffMagicGuard.HasReferenceId(Constants.Magician.Skills.MagicGuard) &&
                    chr.PrimaryStats.MP > 0)
                {
                    // Absorbs X amount of damage. :)
                    var skillId = chr.PrimaryStats.BuffMagicGuard.R;
                    byte skillLevel;
                    var sld = chr.Skills.GetSkillLevelData(skillId, out skillLevel);

                    int damageEaten = (int)Math.Round((damage * (sld.XValue / 100.0d)));

                    // MagicGuard doesn't show reduced damage.


                    Trace.WriteLine($"Reducing damage by MG. Reflected {damageEaten}");

                    //Program.MainForm.LogAppend("MG Damage before change: " + actualHPEffect);
                    actualHPEffect += damageEaten;
                    //Program.MainForm.LogAppend("MG Damage after change: " + actualHPEffect);
                    actualMPEffect = -damageEaten;

                    healSkillId = skillId;
                }

                if (chr.PrimaryStats.BuffPowerGuard.HasReferenceId(Constants.Fighter.Skills.PowerGuard) ||
                    chr.PrimaryStats.BuffPowerGuard.HasReferenceId(Constants.Page.Skills.PowerGuard))
                {
                    var skillId = chr.PrimaryStats.BuffPowerGuard.R;
                    byte skillLevel;
                    var sld = chr.Skills.GetSkillLevelData(skillId, out skillLevel);

                    int damageReflectedBack = (int)(damage * (sld.XValue / 100.0d));

                    if (damageReflectedBack > mob.MaxHP)
                        damageReflectedBack = (int)(mob.MaxHP * 0.1);

                    if (mob.IsBoss)
                        damageReflectedBack /= 2;

                    mob.GiveDamage(chr, damageReflectedBack);
                    MobPacket.SendMobDamageOrHeal(chr, mobId, damageReflectedBack, false, false);

                    mob.CheckDead(mob.Position);

                    Trace.WriteLine($"Reducing damage by PG. Reflected {damageReflectedBack}");
                    actualHPEffect += damageReflectedBack; // Buff 'damaged' hp, so its less
                    healSkillId = skillId;
                }

                
                if (chr.PrimaryStats.BuffMesoGuard.IsSet())
                {
                    var skillId = Constants.ChiefBandit.Skills.MesoGuard;
                    var sld = chr.Skills.GetSkillLevelData(
                        skillId,
                        out var skillLevel
                    );

                    if (sld != null)
                    {
                        var percentage = sld.XValue;

                        var damageReduction = reducedDamage / 2;
                        var mesoLoss = damageReduction * percentage / 100;
                        if (damageReduction != 0)
                        {
                            var playerMesos = chr.Inventory.Mesos;
                            var maxMesosUsable = Math.Min(chr.PrimaryStats.BuffMesoGuard.MesosLeft, playerMesos);
                            if (mesoLoss > maxMesosUsable)
                            {
                                // New calculation. in our version it should actually 'save' the
                                // mesos for a bit.
                                damageReduction = 100 * maxMesosUsable / percentage;
                                mesoLoss = maxMesosUsable;
                            }

                            if (mesoLoss > 0)
                            {
                                chr.PrimaryStats.BuffMesoGuard.MesosLeft -= mesoLoss;
                                MesosTransfer.PlayerUsedSkill(chr.ID, mesoLoss, skillId);

                                chr.AddMesos(-(mesoLoss), false);

                                Trace.WriteLine($"Reducing damage by mesos. Mesos: {mesoLoss}, maxMesos {maxMesosUsable}, reduction {damageReduction}");
                                actualHPEffect += damageReduction;
                                reducedDamage -= reducedDamage;
                            }

                            if (chr.PrimaryStats.BuffMesoGuard.MesosLeft <= 0)
                            {
                                // Debuff when out of mesos
                                chr.PrimaryStats.RemoveByReference(skillId);
                            }
                        }
                    }
                }
                
                SendCharacterDamageByMob(
                    chr,
                    attack,
                    damage,
                    reducedDamage,
                    healSkillId,
                    mobMapId,
                    mobId,
                    stance,
                    isReflected,
                    reflectHitAction,
                    reflectX,
                    reflectY
                );

            }

            Trace.WriteLine($"Showing damage: {reducedDamage}, {damage}");
            Trace.WriteLine($"Applying damage: HP {actualHPEffect}, MP: {actualMPEffect}");

            if (actualHPEffect < 0) chr.ModifyHP((short)actualHPEffect);
            if (actualMPEffect < 0) chr.ModifyMP((short)actualMPEffect);

            if (mobSkillLevel != 0 && mobSkillId != 0)
            {
                // Check if the skill exists and has any extra effect.

                if (!DataProvider.MobSkills.TryGetValue(mobSkillId, out var skillLevels)) return;

                // Still going strong
                if (!skillLevels.TryGetValue(mobSkillLevel, out var msld)) return;
                OnStatChangeByMobSkill(chr, msld);
            }
            else if (mob != null)
            {
                // CUser::OnStatChangeByMobAttack
                if (mob.Data.Attacks == null ||
                    !mob.Data.Attacks.TryGetValue((byte)attack, out var mad)) return;
                // Okay, we've got an attack...
                if (mad.Disease <= 0) return;

                // Shit's poisonous!
                // Hmm... We could actually make snails give buffs... hurr

                if (!DataProvider.MobSkills.TryGetValue(mad.Disease, out var skillLevels)) return;

                // Still going strong
                if (!skillLevels.TryGetValue(mad.SkillLevel, out var msld)) return;
                OnStatChangeByMobSkill(chr, msld);
            }
        }

        public static void OnStatChangeByMobSkill(Character chr, MobSkillLevelData msld, short delay = 0)
        {

            // See if we can actually set the effect...
            int prop = 100;
            if (msld.Prop != 0)
                prop = msld.Prop;

            if (Rand32.Next() % 100 >= prop) return; // Luck.

            BuffStat setStat = null;
            int rValue = msld.SkillID | (msld.Level << 16);
            var ps = chr.PrimaryStats;
            int nValue = 1;
            switch ((Constants.MobSkills.Skills)msld.SkillID)
            {
                case Constants.MobSkills.Skills.Seal: setStat = ps.BuffSeal; break;
                case Constants.MobSkills.Skills.Darkness: setStat = ps.BuffDarkness; break;
                case Constants.MobSkills.Skills.Weakness: setStat = ps.BuffWeakness; break;
                case Constants.MobSkills.Skills.Stun: setStat = ps.BuffStun; break;
                case Constants.MobSkills.Skills.Curse: setStat = ps.BuffCurse; break;
                case Constants.MobSkills.Skills.Poison:
                    setStat = ps.BuffPoison;
                    nValue = msld.X;
                    break;
            }

            if (setStat != null && !setStat.IsSet())
            {
                var buffTime = msld.Time * 1000;
                var stat = setStat.Set(rValue, (short)nValue, BuffStat.GetTimeForBuff(buffTime + delay));

                if (stat != 0)
                {
                    chr.Buffs.FinalizeBuff(stat, delay);
                }
            }
        }

        public static void SendCharacterDamageByMob(
            Character chr,
            sbyte attack,
            int initialDamage,
            int reducedDamage,
            int healSkillId,
            int mobMapId,
            int mobId,
            byte stance,
            bool isReflected,
            byte reflectHitAction,
            short reflectX,
            short reflectY)
        {
            Packet pw = new Packet(ServerMessages.DAMAGE_PLAYER);
            pw.WriteInt(chr.ID);
            pw.WriteSByte(attack);
            pw.WriteInt(initialDamage);

            pw.WriteInt(mobMapId);
            pw.WriteInt(mobId);
            pw.WriteByte(stance);
            pw.WriteBool(isReflected);
            if (isReflected)
            {
                pw.WriteByte(reflectHitAction);
                pw.WriteShort(reflectX);
                pw.WriteShort(reflectY);
            }

            pw.WriteInt(reducedDamage);
            // Not used in client
            // if (reducedDamage < 0) pw.WriteInt(healSkillId);

            chr.Field.SendPacket(chr, pw);
        }

        public static void SendCharacterDamage(Character chr, sbyte attack, int initialDamage, int reducedDamage, int healSkillId)
        {
            Packet pw = new Packet(ServerMessages.DAMAGE_PLAYER);
            pw.WriteInt(chr.ID);
            pw.WriteSByte(attack);
            pw.WriteInt(initialDamage);

            pw.WriteInt(reducedDamage);
            // Not used in client
            // if (reducedDamage < 0) pw.WriteInt(healSkillId);

            chr.Field.SendPacket(chr, pw);
        }

        public static void SendGainEXP(Character chr, int amount, bool IsLastHit, bool Quest = false)
        {
            Packet pw = new Packet(ServerMessages.SHOW_STATUS_INFO);
            pw.WriteByte(3);
            pw.WriteBool(IsLastHit);
            pw.WriteInt(amount);
            pw.WriteBool(Quest);
            chr.SendPacket(pw);
        }

        public static void SendGainDrop(Character chr, bool isMesos, int idOrMesosAmount, short amount)
        {
            Packet pw = new Packet(ServerMessages.SHOW_STATUS_INFO);
            pw.WriteByte(0x00);
            pw.WriteBool(isMesos);
            pw.WriteInt(idOrMesosAmount);

            if (!isMesos)
            {
                byte inv = (byte)(idOrMesosAmount / 1000000);
                pw.WriteInt(inv == 1 ? 1 : amount);
            }
            chr.SendPacket(pw);
        }

    }
}