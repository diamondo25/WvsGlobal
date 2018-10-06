using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;
using WvsBeta.Game.GameObjects;

namespace WvsBeta.Game
{
    public static class AttackPacket
    {
        public static byte IncrementFromStage = 0;

        public enum AttackTypes
        {
            Melee,
            Ranged,
            Magic,
            Summon
        }

        public static bool ParseAttackData(Character chr, Packet packet, out AttackData data, AttackTypes type)
        {
            // Don't accept zombies
            if (chr.PrimaryStats.HP == 0)
            {
                data = null;
                return false;
            }

            // Normal weapon + sword boost gives about 500, so we need to go faster
            if (false && packet.PacketCreationTime - chr.LastAttackPacket < 350)
            {
                Trace.WriteLine($"{packet.PacketCreationTime - chr.LastAttackPacket}");
                if (chr.AssertForHack(chr.FastAttackHackCount++ > 5, $"Fast attack hack, type {type}"))
                {
                    chr.FastAttackHackCount = 0;
                    data = null;
                    return false;
                }
            }
            chr.LastAttackPacket = packet.PacketCreationTime;


            AttackData ad = new AttackData();
            byte hits;
            byte targets;
            int skillid = 0;
            data = null;

            if (type != AttackTypes.Summon)
            {
                ad.RandomNumber = chr.RndActionRandomizer.Random();

                byte TByte = packet.ReadByte();
                skillid = packet.ReadInt();

                if (skillid != 0)
                {
                    if (!chr.Skills.Skills.ContainsKey(skillid))
                    {
                        return false; // Hacks
                    }

                    ad.SkillLevel = chr.Skills.Skills[skillid];
                }
                else
                {
                    ad.SkillLevel = 0;
                }

                if (skillid == Constants.ChiefBandit.Skills.MesoExplosion)
                {
                    ad.IsMesoExplosion = true;
                }

                targets = (byte)(TByte / 0x10);
                hits = (byte)(TByte % 0x10);

                ad.Option = packet.ReadByte();
                var b = packet.ReadByte();
                ad.Action = (byte)(b & 0x7F);
                ad.FacesLeft = (b >> 7) == 1;
                ad.AttackType = packet.ReadByte();
            }
            else
            {
                ad.SummonID = packet.ReadInt();
                ad.AttackType = packet.ReadByte();
                targets = 1;
                hits = 1;
            }

            if (type == AttackTypes.Ranged)
            {
                ad.StarItemSlot = packet.ReadShort();
                BaseItem item = chr.Inventory.GetItem(2, ad.StarItemSlot);

                if (ad.StarItemSlot == 0)
                {
                    if (!chr.PrimaryStats.BuffSoulArrow.IsSet())
                    {
                        chr.DesyncedSoulArrows++;
                        if (chr.AssertForHack(chr.DesyncedSoulArrows > 5, "Trying to use no arrow without Soul Arrow " + chr.DesyncedSoulArrows + " times.")) //Allow up to 5 arrows due to buff desync
                        {
                            return false;
                        }
                    }
                    else
                    {
                        chr.DesyncedSoulArrows = 0;
                    }
                    ad.StarID = -1;
                }
                else if (chr.AssertForHack(item == null, "Attempting to use nonexistent item for ranged attack"))
                {
                    return false;
                }
                else
                {
                    ad.StarID = item.ItemID;
                }

                ad.ShootRange = packet.ReadByte();
            }

            ad.Targets = targets;
            ad.Hits = hits;
            ad.SkillID = skillid;

            for (byte i = 0; i < targets; i++)
            {
                var ai = new AttackData.AttackInfo()
                {
                    MobMapId = packet.ReadInt(),
                    HitAction = packet.ReadByte()
                };
                var b = packet.ReadByte();
                ai.ForeAction = (byte)(b & 0x7F);
                ai.FacesLeft = b >> 7 == 1;
                ai.FrameIndex = packet.ReadByte();
                if (!ad.IsMesoExplosion)
                {
                    ai.CalcDamageStatIndex = packet.ReadByte();
                }
                ai.HitPosition = new Pos(packet);
                ai.PreviousMobPosition = new Pos(packet);

                if (type == AttackTypes.Summon)
                {
                    packet.Skip(1);
                }

                if (ad.IsMesoExplosion)
                {
                    hits = packet.ReadByte();
                }

                ai.Damages = new List<int>(hits);
                if (ad.IsMesoExplosion)
                {
                    // Hit delay is basically set to 0, because
                    // they did not send it to the server.
                    // Technically they use the previous hit info,
                    // but that would be also set to zero... wut

                    for (byte j = 0; j < hits; j++)
                    {
                        int dmg = packet.ReadInt();
                        ad.TotalDamage += dmg;
                        ai.Damages.Add(dmg);
                    }
                }
                else
                {
                    if (type != AttackTypes.Summon)
                    {
                        ai.HitDelay = packet.ReadShort();
                    }

                    for (byte j = 0; j < hits; j++)
                    {
                        int dmg = packet.ReadInt();
                        ad.TotalDamage += dmg;
                        ai.Damages.Add(dmg);
                    }
                }
                ad.Attacks.Add(ai);
            }

            ad.PlayerPosition = new Pos(packet);

            data = ad;


            if (ad.Hits != 0)
            {
                foreach (var ai in ad.Attacks)
                {
                    var mobId = chr.Field.GetMob(ai.MobMapId)?.MobID;
                    if (mobId == null) continue;

                    // Make sure we update the damage log.
                    chr.UpdateDamageLog(
                        ad.SkillID,
                        ad.SkillLevel,
                        mobId.Value,
                        ai.Damages.Min(),
                        ai.Damages.Max()
                    );
                }
            }

            return true;
        }

        public static void HandleMeleeAttack(Character chr, Packet packet)
        {
            //Program.MainForm.LogAppend("Handling Melee");
            if (!ParseAttackData(chr, packet, out AttackData ad, AttackTypes.Melee)) return;

            SendMeleeAttack(chr, ad);
            Mob mob;
            bool died;
            int TotalDamage = 0;
            double MaxPossibleDamage;

            if (ad.SkillID != 0)
            {
                chr.Skills.UseMeleeAttack(ad.SkillID, ad);
            }

            bool pickPocketActivated = chr.PrimaryStats.HasBuff(Constants.ChiefBandit.Skills.Pickpocket);
            var pickPocketSLD = chr.Skills.GetSkillLevelData(Constants.ChiefBandit.Skills.Pickpocket, out byte pickPocketSkillLevel);
            bool pickOk = !ad.IsMesoExplosion && pickPocketActivated && pickPocketSkillLevel > 0 && pickPocketSLD != null;

            int StolenMP = 0;
            int MpStealSkillID = chr.Skills.GetMpStealSkillData(2, out int MpStealProp, out int MpStealPercent, out byte MpStealLevel);

            List<Drop> dropsToPop = null;
            short delayForMesoExplosionKill = 0;
            if (ad.SkillID == Constants.ChiefBandit.Skills.MesoExplosion)
            {
                byte items = packet.ReadByte();
                dropsToPop = new List<Drop>(items);
                byte i;
                for (i = 0; i < items; i++)
                {
                    int objectID = packet.ReadInt();
                    packet.Skip(1);

                    if (chr.Field.DropPool.Drops.TryGetValue(objectID, out var drop) &&
                        drop.Reward.Mesos)
                    {
                        dropsToPop.Add(drop);
                    }
                }

                delayForMesoExplosionKill = packet.ReadShort();

            }


            var sld = ad.SkillID == 0 ? null : DataProvider.Skills[ad.SkillID].Levels[ad.SkillLevel];
            long buffTime = sld?.BuffTime * 1000 ?? 0;
            long buffExpireTime = MasterThread.CurrentTime + buffTime;
            bool IsSuccessRoll() => sld != null && (Rand32.Next() % 100) < sld.Property;


            foreach (var ai in ad.Attacks)
            {
                try
                {
                    TotalDamage = 0;
                    mob = chr.Field.GetMob(ai.MobMapId);

                    if (mob == null) continue;

                    bool boss = mob.Data.Boss;

                    if (MpStealPercent > 0)
                        StolenMP += mob.OnMobMPSteal(MpStealProp, MpStealPercent / ad.Targets);
                    if (pickOk)
                        mob.GiveMoney(chr, ai, ad.Hits);

                    foreach (var amount in ai.Damages)
                    {
                        mob.GiveDamage(chr, amount);
                        TotalDamage += amount;
                    }

                    if (TotalDamage == 0) continue;

                    var maxDamage = 5 + (chr.Level * 6);
                    if (ad.SkillID == 0 && chr.Level < 10 && TotalDamage > maxDamage)
                    {
                        chr.PermaBan("Melee damage hack (low level), hit " + TotalDamage + " (max: " + maxDamage + ")");
                        return;
                    }

                    died = mob.CheckDead(ai.HitPosition, ad.IsMesoExplosion ? delayForMesoExplosionKill : ai.HitDelay, chr.PrimaryStats.BuffMesoUP.N);

                    //TODO sometimes when attacking without using a skill this gets triggered and throws a exception?
                    if (died || ad.SkillID <= 0) continue;

                    if (ad.SkillID != 0)
                    {

                        MobStatus.MobStatValue addedStats = 0;

                        switch (ad.SkillID)
                        {

                            case Constants.DragonKnight.Skills.Sacrifice:
                                {
                                    double percentSacrificed = sld.XValue / 100.0;
                                    short amountSacrificed = (short)(TotalDamage * percentSacrificed);
                                    chr.DamageHP(amountSacrificed);
                                    break;
                                }
                            case Constants.Bandit.Skills.Steal:
                                if (!boss && IsSuccessRoll())
                                    mob.GiveReward(chr.ID, 0, DropType.Normal, ai.HitPosition, ai.HitDelay, 0, true);
                                break;

                            // Debuffs

                            case Constants.Rogue.Skills.Disorder:

                                addedStats = mob.Status.BuffPhysicalDamage.Set(ad.SkillID, (short)sld.XValue, buffExpireTime);
                                addedStats |= mob.Status.BuffPhysicalDefense.Set(ad.SkillID, (short)sld.XValue, buffExpireTime);
                                break;

                            case Constants.WhiteKnight.Skills.ChargeBlow: // Not sure if this should add the stun
                            case Constants.Crusader.Skills.AxeComa:
                            case Constants.Crusader.Skills.SwordComa:
                            case Constants.Crusader.Skills.Shout:
                                if (!boss && IsSuccessRoll())
                                {
                                    addedStats = mob.Status.BuffStun.Set(ad.SkillID, (short)-sld.BuffTime, buffExpireTime);
                                }
                                //is charge blow supposed to end the elemental charge buff?
                                break;

                            case Constants.Crusader.Skills.AxePanic:
                            case Constants.Crusader.Skills.SwordPanic:
                                if (!boss && IsSuccessRoll())
                                {
                                    addedStats = mob.Status.BuffDarkness.Set(ad.SkillID, (short)1, buffExpireTime);
                                    //darkness animation doesnt show in this ver?
                                }
                                break;

                        }

                        if (addedStats != 0)
                        {
                            MobPacket.SendMobStatsTempSet(mob, ai.HitDelay, addedStats);
                        }
                    }


                    if (StolenMP > 0)
                    {
                        chr.ModifyMP((short)StolenMP);
                        MapPacket.SendPlayerSkillAnimSelf(chr, MpStealSkillID, MpStealLevel);
                        MapPacket.SendPlayerSkillAnim(chr, MpStealSkillID, MpStealLevel);
                    }

                    if (ad.SkillID != 0)
                    {
                        MaxPossibleDamage = DamageFormula.MaximumMeleeDamage(chr, mob, ad.Targets, ad.SkillID);
                    }
                    else
                    {
                        MaxPossibleDamage = DamageFormula.MaximumMeleeDamage(chr, mob, ad.Targets);
                    }

                    if (TotalDamage > MaxPossibleDamage)
                    {
                        if (ReportDamagehack(chr, ad, TotalDamage, (int)MaxPossibleDamage)) return;
                    }
                }

                catch (Exception ex)
                {
                    Program.MainForm.LogAppend(ex.ToString());
                }
            }

            if (chr.PrimaryStats.BuffComboAttack.IsSet() && TotalDamage > 0)
            {
                if (ad.SkillID == Constants.Crusader.Skills.AxeComa ||
                    ad.SkillID == Constants.Crusader.Skills.SwordComa ||
                    ad.SkillID == Constants.Crusader.Skills.AxePanic ||
                    ad.SkillID == Constants.Crusader.Skills.SwordPanic)
                {
                    chr.PrimaryStats.BuffComboAttack.N = 1;
                    BuffPacket.SetTempStats(chr, BuffValueTypes.ComboAttack);
                    MapPacket.SendPlayerBuffed(chr, BuffValueTypes.ComboAttack);
                }
                else if (ad.SkillID != Constants.Crusader.Skills.Shout)
                {
                    if (chr.PrimaryStats.BuffComboAttack.N <= chr.PrimaryStats.BuffComboAttack.MaxOrbs)
                    {
                        chr.PrimaryStats.BuffComboAttack.N++;
                        BuffPacket.SetTempStats(chr, BuffValueTypes.ComboAttack);
                        MapPacket.SendPlayerBuffed(chr, BuffValueTypes.ComboAttack);
                    }
                }
            }


            switch (ad.SkillID)
            {
                case 0: // Normal wep
                    {
                        if (chr.Inventory.GetEquippedItemId((short)Constants.EquipSlots.Slots.Helm, true) == 1002258) // Blue Diamondy Bandana
                        {
                            var mobs = chr.Field.GetMobsInRange(chr.Position, new Pos(-10000, -10000), new Pos(10000, 10000));

                            foreach (var m in mobs)
                            {
                                MobPacket.SendMobDamageOrHeal(chr.Field, m, 1337, false, false);

                                if (m.GiveDamage(chr, 1337))
                                {
                                    m.CheckDead();
                                }
                            }
                        }
                        break;
                    }

                case Constants.ChiefBandit.Skills.MesoExplosion:
                    {
                        byte i = 0;
                        foreach (var drop in dropsToPop)
                        {
                            var delay = (short)Math.Min(1000, delayForMesoExplosionKill + (100 * (i % 5)));
                            chr.Field.DropPool.RemoveDrop(drop, RewardLeaveType.Explode, delay);
                            i++;
                        }
                        break;
                    }

                case Constants.WhiteKnight.Skills.ChargeBlow:
                    if (IsSuccessRoll())
                    {
                        // RIP. It cancels your charge
                        var removedBuffs = chr.PrimaryStats.RemoveByReference(chr.PrimaryStats.BuffCharges.R);
                        BuffPacket.SetTempStats(chr, removedBuffs);
                        MapPacket.SendPlayerBuffed(chr, removedBuffs);
                    }
                    break;

                case Constants.WhiteKnight.Skills.BwFireCharge:
                case Constants.WhiteKnight.Skills.BwIceCharge:
                case Constants.WhiteKnight.Skills.BwLitCharge:
                case Constants.WhiteKnight.Skills.SwordFireCharge:
                case Constants.WhiteKnight.Skills.SwordIceCharge:
                case Constants.WhiteKnight.Skills.SwordLitCharge:
                    {
                        var buff = chr.PrimaryStats.BuffCharges.Set(
                            ad.SkillID,
                            sld.XValue,
                            BuffStat.GetTimeForBuff(1000 * sld.BuffTime)
                        );
                        BuffPacket.SetTempStats(chr, buff);
                        MapPacket.SendPlayerBuffed(chr, buff);
                        break;
                    }

                case Constants.DragonKnight.Skills.DragonRoar:
                    {
                        // Apply stun
                        var buff = chr.PrimaryStats.BuffStun.Set(
                            ad.SkillID,
                            1,
                            BuffStat.GetTimeForBuff(1000 * sld.YValue)
                        );
                        BuffPacket.SetTempStats(chr, buff);
                        MapPacket.SendPlayerBuffed(chr, buff);
                        break;
                    }
            }

        }

        public static void HandleRangedAttack(Character chr, Packet packet)
        {
            //Program.MainForm.LogAppend("Handling Ranged");
            if (!ParseAttackData(chr, packet, out AttackData ad, AttackTypes.Ranged)) return;

            int TotalDamage;
            bool died;
            double MaxPossibleDamage = 0;
            Mob mob;

            SendRangedAttack(chr, ad);

            if (ad.SkillID != 0 || ad.StarID != 0)
            {
                chr.Skills.UseRangedAttack(ad.SkillID, ad.StarItemSlot);
            }

            foreach (var ai in ad.Attacks)
            {
                try
                {
                    TotalDamage = 0;
                    mob = chr.Field.GetMob(ai.MobMapId);

                    if (mob != null)
                    {
                        foreach (int amount in ai.Damages)
                        {
                            mob.GiveDamage(chr, amount);
                            TotalDamage += amount;
                        }

                        if (TotalDamage != 0)
                        {
                            died = mob.CheckDead(ai.HitPosition, ai.HitDelay, chr.PrimaryStats.BuffMesoUP.N);


                            var sld = chr.Skills.GetSkillLevelData(ad.SkillID, out byte derp);
                            if (sld != null && derp == ad.SkillLevel)
                            {
                                long buffTime = sld.BuffTime * 1000;

                                if (!died)
                                {
                                    if (ad.SkillID == Constants.Hunter.Skills.ArrowBomb && !mob.IsBoss)
                                    {
                                        short chance = RNG.Range.generate((short)0, (short)100);

                                        if (chance < sld.Property)
                                        {
                                            var stat = mob.Status.BuffStun.Set(ad.SkillID, 1,
                                                MasterThread.CurrentTime + buffTime);
                                            MobPacket.SendMobStatsTempSet(mob, ai.HitDelay, stat);
                                        }
                                    }
                                }

                                if (ad.SkillID == Constants.Assassin.Skills.Drain)
                                {
                                    double hp = Math.Min(ai.Damages[0] * sld.XValue * 0.01, mob.MaxHP);
                                    hp = Math.Min(hp, chr.PrimaryStats.MaxHP / 2);
                                    chr.ModifyHP((short)(hp));
                                }

                                switch (ad.SkillID)
                                {
                                    case Constants.Hunter.Skills.PowerKnockback:
                                    case Constants.Crossbowman.Skills.PowerKnockback:
                                        MaxPossibleDamage = DamageFormula.MaximumPowerKnockbackDamage(chr, mob, TotalDamage);
                                        break;
                                    case Constants.Hunter.Skills.ArrowBomb:
                                        MaxPossibleDamage =
                                            DamageFormula.MaximumArrowBombDamage(chr, mob, ad.StarID, TotalDamage);
                                        break;
                                    case Constants.Rogue.Skills.LuckySeven:
                                        MaxPossibleDamage = DamageFormula.MaximumLuckySevenDamage(chr, mob, TotalDamage);
                                        break;
                                    case Constants.Assassin.Skills.Drain:
                                        MaxPossibleDamage = 99999; //Hotfix a bug that caused legit players to get autobanned. TODO check this properly!
                                        break;
                                    default:
                                        MaxPossibleDamage = DamageFormula.MaximumRangedDamage(chr, mob, ad.SkillID, ad.StarID,
                                            ad.Targets, TotalDamage);
                                        break;
                                }

                                if (TotalDamage > MaxPossibleDamage)
                                {
                                    if (ReportDamagehack(chr, ad, TotalDamage, (int)MaxPossibleDamage)) return;
                                }
                            }
                        }
                    }
                }

                catch (Exception ex)
                {
                    //Program.MainForm.LogAppendFormat(ex.ToString());
                }
            }
        }

        public static void HandleMagicAttack(Character chr, Packet packet)
        {
            //Program.MainForm.LogAppend("Handling Magic");
            if (!ParseAttackData(chr, packet, out AttackData ad, AttackTypes.Magic)) return;

            int TotalDamage;
            double MaxSpellDamage;
            bool died;
            Mob mob;

            SendMagicAttack(chr, ad);


            if (ad.SkillID != 0)
            {
                chr.Skills.UseMeleeAttack(ad.SkillID, ad);
            }

            int StolenMP = 0;
            int MpStealSkillID = chr.Skills.GetMpStealSkillData(2, out int MpStealProp, out int MpStealPercent, out byte MpStealLevel);

            foreach (var ai in ad.Attacks)
            {
                try
                {
                    TotalDamage = 0;
                    mob = chr.Field.GetMob(ai.MobMapId);

                    if (mob == null) continue;

                    foreach (int amount in ai.Damages)
                    {
                        mob.GiveDamage(chr, amount);
                        TotalDamage += amount;
                    }

                    if (MpStealPercent > 0)
                        StolenMP += mob.OnMobMPSteal(MpStealProp, MpStealPercent / ad.Targets);

                    if (TotalDamage == 0) continue;

                    died = mob.CheckDead(ai.HitPosition, ai.HitDelay, chr.PrimaryStats.BuffMesoUP.N);

                    if (!died)
                    {
                        var sld = DataProvider.Skills[ad.SkillID].Levels[ad.SkillLevel];
                        long buffTime = sld.BuffTime * 1000;

                        //TODO refactor element code when we get the proper element loading with calcdamage branch
                        if ((sld.ElementFlags == SkillElement.Ice || ad.SkillID == Constants.ILMage.Skills.ElementComposition) && !mob.IsBoss)
                        {
                            var stat = mob.Status.BuffFreeze.Set(ad.SkillID, 1, MasterThread.CurrentTime + buffTime);
                            MobPacket.SendMobStatsTempSet(mob, ai.HitDelay, stat);
                        }

                        if (ad.SkillID == Constants.FPMage.Skills.ElementComposition && !mob.IsBoss)
                        {
                            if (Rand32.NextBetween(0, 100) < sld.Property)
                            {
                                mob.DoPoison(chr.ID, chr.Skills.GetSkillLevel(ad.SkillID), buffTime, ad.SkillID, sld.MagicAttack, ai.HitDelay);
                            }
                        }

                        if (ad.SkillID == Constants.FPWizard.Skills.PoisonBreath)
                        {
                            if (Rand32.NextBetween(0, 100) < sld.Property)
                            {
                                mob.DoPoison(chr.ID, chr.Skills.GetSkillLevel(ad.SkillID), buffTime, ad.SkillID, sld.MagicAttack, ai.HitDelay);
                            }
                        }

                        if (StolenMP > 0)
                        {
                            chr.ModifyMP((short)StolenMP);
                            MapPacket.SendPlayerSkillAnimSelf(chr, MpStealSkillID, MpStealLevel);
                            MapPacket.SendPlayerSkillAnim(chr, MpStealSkillID, MpStealLevel);
                        }
                    }

                    switch (ad.SkillID)
                    {
                        case Constants.Magician.Skills.MagicClaw: // If the Spell is Magic Claw (since it registers two hits)
                            MaxSpellDamage = DamageFormula.MaximumSpellDamage(chr, mob, ad.SkillID) * 2;
                            break;
                        case Constants.Cleric.Skills.Heal: // If the Spell is Heal (since it has a special snowflake calculation for it)
                            if (mob.Data.Undead == false)
                            {
                                chr.PermaBan("Heal damaging regular mobs exploit");
                                return;
                            }
                            MaxSpellDamage = DamageFormula.MaximumHealDamage(chr, mob, ad.Targets);
                            break;
                        default:
                            MaxSpellDamage = DamageFormula.MaximumSpellDamage(chr, mob, ad.SkillID);
                            break;
                    }

                    if (TotalDamage > MaxSpellDamage)
                    {
                        if (ReportDamagehack(chr, ad, TotalDamage, (int)MaxSpellDamage)) return;
                    }
                }

                catch (Exception ex)
                {
                    Program.MainForm.LogAppend(ex.ToString());
                }
            }
        }

        
        public static void HandleSummonAttack(Character chr, Packet packet)
        {
            if (!ParseAttackData(chr, packet, out AttackData ad, AttackTypes.Summon)) return;

            var summonId = ad.SummonID;

            if (chr.Summons.GetSummon(summonId, out var summon))
            {
                //SendMagicAttack(chr, ad);
                SendSummonAttack(chr, summon, ad);
                var totalDamage = 0;
                foreach (var ai in ad.Attacks)
                {
                    try
                    {
                        var mob = chr.Field.GetMob(ai.MobMapId);

                        if (mob != null)
                        {
                            foreach (int amount in ai.Damages)
                            {
                                totalDamage += amount;
                                mob.GiveDamage(chr, amount);
                            }

                            var dead = mob.CheckDead(mob.Position, ai.HitDelay, chr.PrimaryStats.BuffMesoUP.N);

                            if (!dead)
                            {
                                switch(summon.SkillId)
                                {
                                    case Constants.Ranger.Skills.SilverHawk:
                                    case Constants.Sniper.Skills.GoldenEagle:
                                        var sld = CharacterSkills.GetSkillLevelData(summon.SkillId, summon.SkillLevel);
                                        if (!mob.IsBoss && totalDamage > 0 && Rand32.NextBetween(0, 100) < sld.Property)
                                        {
                                            int stunTime = ai.HitDelay + 4000;
                                            long expireTime = MasterThread.CurrentTime + stunTime;
                                            var stat = mob.Status.BuffStun.Set(summon.SkillId, -1, expireTime);
                                            MobPacket.SendMobStatsTempSet(mob, ai.HitDelay, stat);
                                        }
                                        break;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Program.MainForm.LogAppend(ex.ToString());
                    }
                }
            }
        }

        public static void SendSummonAttack(Character chr, Summon summon, AttackData ad)
        {
            Packet pw = new Packet(ServerMessages.SPAWN_ATTACK);
            pw.WriteInt(chr.ID);
            pw.WriteInt(summon.SkillId);
            pw.WriteByte(ad.AttackType);
            pw.WriteByte(ad.Targets);
            foreach (var kvp in ad.Attacks)
            {
                pw.WriteInt(kvp.MobMapId);
                pw.WriteByte(kvp.HitAction);
                foreach (int dmg in kvp.Damages)
                {
                    pw.WriteInt(dmg);
                }
            }
            chr.Field.SendPacket(pw, chr);
        }

        private static int? GetFixedDamage(Character chr)
        {
            int? fixedDamage = null;

            /*switch (chr.Name)
            {
                case "Exile": fixedDamage = 2147483647; break;
                case "Diamondo25": fixedDamage = 25252525; break;
                case "wackyracer": fixedDamage = 13371337; break;
            }*/

            return fixedDamage;
        }

        public static void SendMeleeAttack(Character chr, AttackData data)
        {
            var fixedDamage = GetFixedDamage(chr);
            byte tbyte = (byte)((data.Targets * 0x10) + data.Hits);

            Packet pw = new Packet(ServerMessages.CLOSE_RANGE_ATTACK);
            pw.WriteInt(chr.ID);
            pw.WriteByte(tbyte);
            pw.WriteByte(data.SkillLevel);

            if (data.SkillLevel != 0)
            {
                pw.WriteInt(data.SkillID);
            }

            pw.WriteByte((byte)(data.Action | (data.FacesLeft ? 1 << 7 : 0)));
            pw.WriteByte(data.AttackType);

            int mastery = chr.Skills.GetMastery();
            pw.WriteByte((byte)(mastery > 0 ? Constants.getMasteryDisplay(chr.Skills.GetSkillLevel(mastery)) : 0));

            pw.WriteInt(data.StarID);

            foreach (var ai in data.Attacks)
            {
                pw.WriteInt(ai.MobMapId);
                pw.WriteByte(ai.HitAction);

                if (data.IsMesoExplosion)
                {
                    pw.WriteByte((byte)ai.Damages.Count);
                }

                foreach (var dmg in ai.Damages)
                {
                    pw.WriteInt(fixedDamage.GetValueOrDefault(dmg));
                }
            }

            chr.Field.SendPacket(chr, pw, chr);
        }

        public static void SendRangedAttack(Character chr, AttackData data)
        {
            var fixedDamage = GetFixedDamage(chr);

            byte tbyte = (byte)((data.Targets * 0x10) + data.Hits);

            Packet pw = new Packet(ServerMessages.RANGED_ATTACK);
            pw.WriteInt(chr.ID);
            pw.WriteByte(tbyte);
            pw.WriteByte(data.SkillLevel);

            if (data.SkillLevel != 0)
            {
                pw.WriteInt(data.SkillID);
            }

            pw.WriteByte((byte)(data.Action | (data.FacesLeft ? 1 << 7 : 0)));
            pw.WriteByte(data.AttackType);

            int mastery = chr.Skills.GetMastery();
            pw.WriteByte((byte)(mastery > 0 ? Constants.getMasteryDisplay(chr.Skills.GetSkillLevel(mastery)) : 0));
            pw.WriteInt(data.StarID);

            foreach (var ai in data.Attacks)
            {
                pw.WriteInt(ai.MobMapId);
                pw.WriteByte(ai.HitAction);

                foreach (var dmg in ai.Damages)
                {
                    pw.WriteInt(fixedDamage.GetValueOrDefault(dmg));
                }
            }

            chr.Field.SendPacket(chr, pw, chr);
        }

        public static void SendMagicAttack(Character chr, AttackData data)
        {
            var fixedDamage = GetFixedDamage(chr);
            byte tbyte = (byte)((data.Targets * 0x10) + data.Hits);

            Packet pw = new Packet(ServerMessages.MAGIC_ATTACK);
            pw.WriteInt(chr.ID);
            pw.WriteByte(tbyte);
            pw.WriteByte(data.SkillLevel);

            if (data.SkillLevel != 0)
            {
                pw.WriteInt(data.SkillID);
            }

            pw.WriteByte((byte)(data.Action | (data.FacesLeft ? 1 << 7 : 0)));
            pw.WriteByte(data.AttackType);

            int mastery = chr.Skills.GetMastery();
            pw.WriteByte((byte)(mastery > 0 ? Constants.getMasteryDisplay(chr.Skills.GetSkillLevel(mastery)) : 0));

            pw.WriteInt(data.StarID);

            foreach (var ai in data.Attacks)
            {
                pw.WriteInt(ai.MobMapId);
                pw.WriteByte(ai.HitAction);

                foreach (var dmg in ai.Damages)
                {
                    pw.WriteInt(fixedDamage.GetValueOrDefault(dmg));
                }
            }

            chr.Field.SendPacket(chr, pw, chr);
        }

        private static ILog _hackLog = LogManager.GetLogger("HackLog");

        public struct DamageHackLog
        {
            public int skillId { get; set; }
            public byte skillLevel { get; set; }
            public int damage { get; set; }
            public int maxDamage { get; set; }
            public float percentage { get; set; }
            public short posX { get; set; }
            public short posY { get; set; }
        }

        private static bool ReportDamagehack(Character chr, AttackData ad, int TotalDamage, int MaxDamage)
        {
            // Broken. don't care.
            return false;


            float percentage = (TotalDamage * 100.0f / MaxDamage);
            _hackLog.Info(new DamageHackLog
            {
                damage = TotalDamage,
                maxDamage = MaxDamage,
                skillId = ad.SkillID,
                skillLevel = ad.SkillLevel,
                percentage = percentage,
                posX = chr.Position.X,
                posY = chr.Position.Y,
            });

            // Ignore broken damage
            if (percentage < 1.0 || TotalDamage < 100 || MaxDamage == 0) return false;

            if (chr.HacklogMuted >= MasterThread.CurrentDate) return false;

            //var msgType = DamageFormula.GetGMNoticeType(TotalDamage, MaxDamage);
            //MessagePacket.SendNoticeGMs("Name: '" + chr.Name + "', dmg: " + TotalDamage + " (" + percentage.ToString("0.00") + "% of max). " + "Skill: " + ad.SkillID + ", lvl " + ad.SkillLevel + ", damage " + TotalDamage + " > " + MaxDamage, msgType);

            if (percentage > 400.0)
            {
                chr.PermaBan($"Automatically banned for damage hacks ({percentage:0.00}%) skill {ad.SkillID}", Character.BanReasons.Hack);
                return true;
            }

            return false;
        }
    }
}