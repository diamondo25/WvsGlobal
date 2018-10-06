using System;
using System.Collections.Generic;
using System.Linq;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;
using WvsBeta.Game.Events.PartyQuests;
using WvsBeta.Game.GameObjects;

namespace WvsBeta.Game
{
    public static class SkillPacket
    {
        public static void HandleUseSkill(Character chr, Packet packet)
        {
            if (chr.PrimaryStats.HP == 0)
            {
                // We don't like zombies
                InventoryPacket.NoChange(chr);
                return;
            }

            var field = chr.Field;

            var SkillID = packet.ReadInt();
            var SkillLevel = packet.ReadByte();


            if (SkillID == Constants.Priest.Skills.MysticDoor && MasterThread.CurrentTime - chr.tLastDoor < 3000)
            {
                //hack fix for door dc bug
                InventoryPacket.NoChange(chr);
                return;
            }

            if (!chr.Skills.Skills.ContainsKey(SkillID) ||
                SkillLevel < 1 ||
                SkillLevel > chr.Skills.Skills[SkillID])
            {
                Program.MainForm.LogAppend("Player {0} tried to use a skill without having it.", chr.ID);
                ReportManager.FileNewReport("Player {0} tried to use a skill without having it.", chr.ID, 0);
                chr.Player.Socket.Disconnect();
                return;
            }

            var isGMHideSkill = SkillID == Constants.Gm.Skills.Hide;

            // Prevent sending the hide enable/disable packet to others
            if (isGMHideSkill)
            {
                chr.SetHide(chr.GMHideEnabled == false, false);
                if (chr.GMHideEnabled == false)
                {
                    StopSkill(chr, SkillID);
                }
            }
            else if (SkillID != Constants.Cleric.Skills.Heal ||
                chr.Inventory.GetEquippedItemId((short)Constants.EquipSlots.Slots.Weapon, false) == 0)
            {
                // If you are using Heal, and are not using a wand/weapon, it won't show anything.

                MapPacket.SendPlayerSkillAnim(chr, SkillID, SkillLevel);
            }

            var sld = DataProvider.Skills[SkillID].Levels[SkillLevel];


            if (SkillID == (int)Constants.Spearman.Skills.HyperBody && !chr.PrimaryStats.HasBuff((int)Constants.Spearman.Skills.HyperBody)) // Buff already exists, do not execute bonus again. Allow multiple casts for duration refresh
            {
                var hpmpBonus = (short)((double)chr.PrimaryStats.MaxHP * ((double)sld.XValue / 100.0d));
                chr.PrimaryStats.BuffBonuses.MaxHP = hpmpBonus;
                hpmpBonus = (short)((double)chr.PrimaryStats.MaxMP * ((double)sld.YValue / 100.0d));
                chr.PrimaryStats.BuffBonuses.MaxMP = hpmpBonus;
            }

            short skillDelay = 0;

            IEnumerable<Character> getCharactersForPartyBuff(byte Flags, bool deadPlayersToo = false)
            {
                if (chr.PartyID == 0) yield break;

                if (PartyData.Parties.TryGetValue(chr.PartyID, out var party))
                {
                    for (var i = 5; i >= 0; i--)
                    {
                        if ((Flags >> (5 - i) & 1) == 0) continue;

                        var charid = party.Members[i];
                        if (charid == 0) continue;

                        var affected = Server.Instance.GetCharacter(charid);

                        if (affected != null && chr.MapID == affected.MapID &&
                            (deadPlayersToo || affected.PrimaryStats.HP > 0))
                        {
                            yield return affected;
                        }
                    }
                }
            }

            void handlePartyEffects(byte Flags, short Delay, bool deadPlayersToo = false, Action<Character> additionalEffects = null)
            {
                handlePartyEffectsWithPlayers(getCharactersForPartyBuff(Flags, deadPlayersToo), Delay, additionalEffects);
            }

            void handlePartyEffectsWithPlayers(IEnumerable<Character> characters, short Delay, Action<Character> additionalEffects = null)
            {
                foreach (var character in characters)
                {
                    if (character == chr) continue;
                    if (!computerSaysYes()) continue;

                    MapPacket.SendPlayerSkillAnimThirdParty(character, SkillID, SkillLevel, true, true);
                    MapPacket.SendPlayerSkillAnimThirdParty(character, SkillID, SkillLevel, true, false);
                    additionalEffects?.Invoke(character);
                }
            }

            // Runs func() for each mob inside the packet that:
            // - is not a boss (when isBossable is false)
            // - effect chance was a success
            void handleMobStatEffects(bool isBossable, Action<Mob, short> func)
            {
                var mobCount = packet.ReadByte();
                var mobs = new List<Mob>(mobCount);

                for (var i = 0; i < mobCount; i++)
                {
                    var mobId = packet.ReadInt();
                    var mob = field.GetMob(mobId);
                    if (mob == null) continue;

                    if (chr.AssertForHack(mob.IsBoss && !isBossable, "Tried hitting boss with non-boss skill", false))
                    {
                        continue;
                    }

                    if (computerSaysYes())
                    {
                        mobs.Add(mob);
                    }
                }

                var delay = packet.ReadShort();

                mobs.ForEach(x => func(x, delay));
            }

            IEnumerable<Character> getFullMapPlayersForGMSkill()
            {
                return field.Characters.Where(victim =>
                {
                    if (victim == chr) return false;
                    if (chr.GMHideEnabled && victim.GMHideEnabled == false) return false;

                    // Only Admins can buff other regular people
                    if (chr.IsGM && !chr.IsAdmin && !victim.IsGM) return false;

                    return true;
                });
            }

            bool computerSaysYes()
            {
                var chance = sld.Property;
                if (chance == 0) chance = 100;
                return !(Rand32.Next() % 100 >= chance);
            }


            //TODO refactor copy-pasted "nearest 6 mobs" logic
            switch (SkillID)
            {
                case Constants.Assassin.Skills.Haste:
                case Constants.Bandit.Skills.Haste:
                case Constants.Cleric.Skills.Bless:
                case Constants.Spearman.Skills.IronWill:
                case Constants.Fighter.Skills.Rage:
                case Constants.FPWizard.Skills.Meditation:
                case Constants.ILWizard.Skills.Meditation:
                case Constants.Hermit.Skills.MesoUp:
                    {
                        var Flags = packet.ReadByte();
                        var Delay = packet.ReadShort();

                        handlePartyEffects(Flags, Delay, false, victim =>
                        {
                            victim.Buffs.AddBuff(SkillID, SkillLevel);
                        });
                        break;
                    }
                case Constants.Spearman.Skills.HyperBody:
                    {
                        var Flags = packet.ReadByte();
                        var Delay = packet.ReadShort();

                        handlePartyEffects(Flags, Delay, false, victim =>
                        {
                            if (!victim.PrimaryStats.HasBuff((int)Constants.Spearman.Skills.HyperBody)) // Buff already exists, do not execute bonus again. Allow multiple casts for duration refresh
                            {
                                var hpmpBonus = (short)((double)victim.PrimaryStats.MaxHP * ((double)sld.XValue / 100.0d));
                                victim.PrimaryStats.BuffBonuses.MaxHP = hpmpBonus;
                                hpmpBonus = (short)((double)victim.PrimaryStats.MaxMP * ((double)sld.YValue / 100.0d));
                                victim.PrimaryStats.BuffBonuses.MaxMP = hpmpBonus;
                            }
                            victim.Buffs.AddBuff(SkillID, SkillLevel);
                        });


                        break;
                    }
                case Constants.Cleric.Skills.Heal:
                    {
                        var Flags = packet.ReadByte();
                        var Delay = packet.ReadShort();
                        var members = getCharactersForPartyBuff(Flags, false);
                        var count = members.Count();

                        double healRate = 0;
                        if (sld.HPProperty > 0)
                        {
                            int chrInt = chr.PrimaryStats.getTotalInt();
                            int chrLuk = chr.PrimaryStats.getTotalLuk();
                            var rate = (chrInt * 0.8) + Rand32.Next() % (chrInt * 0.2);
                            healRate = (((rate * 1.5 + chrLuk) * (chr.PrimaryStats.getTotalMagicAttack() + chr.PrimaryStats.BuffMagicAttack.N) * 0.01) * (count * 0.3 + 1.0) * sld.HPProperty * 0.01);
                        }

                        var bigHeal = Math.Min(((long)healRate / (count == 0 ? 1 : count)), short.MaxValue); //prevent integer overflow caused by high stats. Set count to 1 when not in party
                        var heal = (short)bigHeal;
                        chr.ModifyHP((short)Math.Min(heal, (chr.PrimaryStats.GetMaxHP() - chr.PrimaryStats.HP)));

                        handlePartyEffectsWithPlayers(members, Delay, victim =>
                        {
                            int oldHP = victim.PrimaryStats.HP;
                            victim.ModifyHP((short)Math.Min(heal, (victim.PrimaryStats.GetMaxHP() - victim.PrimaryStats.HP)));
                            chr.AddEXP(20 * ((victim.PrimaryStats.HP - oldHP) / (8 * victim.Level + 190)), true);
                        });

                        break;
                    }
                case Constants.Priest.Skills.Dispel:
                    {
                        var Flags = packet.ReadByte();
                        var Delay = packet.ReadShort();

                        if (computerSaysYes())
                        {
                            chr.Buffs.Dispell();
                        }

                        handlePartyEffects(Flags, Delay, false, victim =>
                        {
                            victim.Buffs.Dispell();
                        });

                        handleMobStatEffects(false, (mob, delay) =>
                        {
                            MobStatus.MobStatValue Flag = 0;
                            Flag |= mob.Status.BuffPowerUp.Reset();
                            Flag |= mob.Status.BuffMagicUp.Reset();
                            Flag |= mob.Status.BuffMagicGuardUp.Reset();
                            Flag |= mob.Status.BuffPowerGuardUp.Reset();
                            Flag |= mob.Status.BuffHardSkin.Reset();
                            MobPacket.SendMobStatsTempReset(mob, Flag);
                        });

                        break;
                    }
                case Constants.Priest.Skills.HolySymbol:
                    {
                        var Flags = packet.ReadByte();
                        var Delay = packet.ReadShort();

                        handlePartyEffects(Flags, Delay, false, victim =>
                        {
                            victim.Buffs.AddBuff(SkillID, SkillLevel);
                        });

                        break;
                    }
                // DOOR
                case Constants.Priest.Skills.MysticDoor:
                    {
                        var x = packet.ReadShort();
                        var y = packet.ReadShort();

                        if (chr.DoorMapId != Constants.InvalidMap)
                        {
                            DataProvider.Maps[chr.DoorMapId].DoorPool.TryRemoveDoor(chr.ID);
                        }

                        chr.DoorMapId = chr.MapID;
                        field.DoorPool.CreateDoor(chr, x, y, MasterThread.CurrentTime + sld.BuffTime * 1000);
                        chr.tLastDoor = MasterThread.CurrentTime;
                        break;
                    }

                // GM SKILLS
                case Constants.Gm.Skills.Haste:
                case Constants.Gm.Skills.HolySymbol:
                case Constants.Gm.Skills.Bless:
                    {
                        getFullMapPlayersForGMSkill().ForEach(victim =>
                        {
                            MapPacket.SendPlayerSkillAnimThirdParty(victim, SkillID, SkillLevel, true, true);
                            MapPacket.SendPlayerSkillAnimThirdParty(victim, SkillID, SkillLevel, true, false);
                            victim.Buffs.AddBuff(SkillID, SkillLevel);
                        });
                        break;
                    }
                case Constants.Gm.Skills.HealPlusDispell:
                    {
                        getFullMapPlayersForGMSkill().ForEach(victim =>
                        {
                            MapPacket.SendPlayerSkillAnimThirdParty(victim, SkillID, SkillLevel, true, true);
                            MapPacket.SendPlayerSkillAnimThirdParty(victim, SkillID, SkillLevel, true, false);
                            victim.ModifyHP(victim.PrimaryStats.GetMaxMP(false), true);
                            victim.ModifyMP(victim.PrimaryStats.GetMaxMP(false), true);
                            victim.Buffs.Dispell();
                        });
                        chr.ModifyHP(chr.PrimaryStats.GetMaxMP(false), true);
                        chr.ModifyMP(chr.PrimaryStats.GetMaxMP(false), true);
                        chr.Buffs.Dispell();
                        break;
                    }
                case Constants.Gm.Skills.Resurrection:
                    {
                        getFullMapPlayersForGMSkill().ForEach(victim =>
                        {
                            if (victim.PrimaryStats.HP <= 0)
                            {
                                MapPacket.SendPlayerSkillAnimThirdParty(victim, SkillID, SkillLevel, true, true);
                                MapPacket.SendPlayerSkillAnimThirdParty(victim, SkillID, SkillLevel, true, false);
                                victim.ModifyHP(victim.PrimaryStats.GetMaxHP(false), true);
                            }
                        });
                        break;
                    }
                // MOB SKILLS
                case Constants.Page.Skills.Threaten:
                    {
                        var buffTime = MasterThread.CurrentTime + (sld.BuffTime * 1000);
                        handleMobStatEffects(false, (mob, delay) =>
                        {
                            var stat = mob.Status.BuffPhysicalDamage.Set(
                                SkillID,
                                (short)-((mob.Data.PAD * SkillLevel) / 100),
                                buffTime + delay
                            );

                            stat |= mob.Status.BuffPhysicalDefense.Set(
                                SkillID,
                                (short)-((mob.Data.PDD * SkillLevel) / 100),
                                buffTime + delay
                            );

                            MobPacket.SendMobStatsTempSet(mob, delay, stat);
                        });
                        break;
                    }
                case Constants.FPWizard.Skills.Slow:
                case Constants.ILWizard.Skills.Slow:
                    {
                        var buffNValue = sld.XValue;
                        var buffTime = MasterThread.CurrentTime + (sld.BuffTime * 1000);

                        handleMobStatEffects(false, (mob, delay) =>
                        {
                            MobPacket.SendMobStatsTempSet(mob, delay, mob.Status.BuffSpeed.Set(SkillID, buffNValue, buffTime + delay));
                        });
                        break;
                    }
                case Constants.Gm.Skills.ItemExplosion:
                    {
                        field.DropPool.Clear(RewardLeaveType.Explode);
                        // TODO: Explode people and such
                        break;
                    }
                case Constants.WhiteKnight.Skills.MagicCrash:
                    {
                        handleMobStatEffects(false, (mob, delay) =>
                        {
                            MobPacket.SendMobStatsTempReset(mob, mob.Status.BuffMagicGuardUp.Reset());
                        });
                        break;
                    }
                case Constants.DragonKnight.Skills.PowerCrash:
                    {
                        handleMobStatEffects(false, (mob, delay) =>
                        {
                            MobPacket.SendMobStatsTempReset(mob, mob.Status.BuffPowerUp.Reset());
                        });
                        break;
                    }
                case Constants.Crusader.Skills.ArmorCrash:
                    {
                        handleMobStatEffects(false, (mob, delay) =>
                        {
                            MobPacket.SendMobStatsTempReset(mob, mob.Status.BuffPowerGuardUp.Reset());
                        });
                        break;
                    }
                case Constants.ILMage.Skills.Seal:
                case Constants.FPMage.Skills.Seal:
                    {
                        var buffTime = MasterThread.CurrentTime + (sld.BuffTime * 1000);
                        handleMobStatEffects(false, (mob, delay) =>
                        {
                            MobPacket.SendMobStatsTempSet(mob, delay, mob.Status.BuffSeal.Set(SkillID, 1, buffTime + delay));
                        });

                        break;
                    }
                case Constants.Hermit.Skills.ShadowWeb:
                    {
                        var buffTime = MasterThread.CurrentTime + (sld.BuffTime * 1000);

                        handleMobStatEffects(false, (mob, delay) =>
                        {
                            var stat = mob.Status.BuffWeb.Set(
                                SkillID,
                                (short)(mob.MaxHP / (50 - SkillLevel)),
                                buffTime + delay
                            );
                            MobPacket.SendMobStatsTempSet(mob, delay, stat);
                        });
                        break;
                    }
                case Constants.Priest.Skills.Doom:
                    {
                        var buffTime = MasterThread.CurrentTime + (sld.BuffTime * 1000);

                        handleMobStatEffects(false, (mob, delay) =>
                        {
                            MobPacket.SendMobStatsTempSet(mob, delay, mob.Status.BuffDoom.Set(SkillID, 1, buffTime + delay));
                        });
                        break;
                    }
                // SUMMONS
                case Constants.Priest.Skills.SummonDragon:
                case Constants.Ranger.Skills.SilverHawk:
                case Constants.Sniper.Skills.GoldenEagle:
                    {
                        var X = packet.ReadShort();
                        var Y = packet.ReadShort();

                        var fh = field.GetFootholdUnderneath(X, Y, out var MaxY);
                        ushort fhid = 0;
                        fhid = fh?.ID ?? (ushort)chr.Foothold;
                        var bird = new Summon(chr, SkillID, SkillLevel, X, Y, true, fhid, MasterThread.CurrentTime + sld.BuffTime * 1000);
                        chr.Summons.SetSummon(bird);

                        break;
                    }
                case Constants.Ranger.Skills.Puppet:
                case Constants.Sniper.Skills.Puppet:
                    {
                        Program.MainForm.LogDebug(packet.ToString());
                        var X = packet.ReadShort();
                        var Y = packet.ReadShort();
                        var fh = field.GetFootholdUnderneath(X, Y, out var MaxY);
                        var floor = field.FindFloor(new Pos(X, Y));
                        ushort fhid = 0;
                        fhid = fh?.ID ?? (ushort)chr.Foothold;
                        var puppet = new Puppet(chr, SkillID, SkillLevel, floor.X, floor.Y, false, fhid, MasterThread.CurrentTime + sld.BuffTime * 1000, sld.XValue);
                        chr.Summons.SetSummon(puppet);
                        break;
                    }
            }

            if (packet.Length == packet.Position + 2)
            {
                // Read the delay...
                skillDelay = packet.ReadShort();
            }

            // Handle regular skill stuff
            if (!isGMHideSkill || chr.GMHideEnabled)
            {
                chr.Buffs.AddBuff(SkillID, SkillLevel, skillDelay);
            }

            InventoryPacket.NoChange(chr);
            chr.Skills.DoSkillCost(SkillID, SkillLevel);

            if (sld.Speed > 0)
            {
                MapPacket.SendAvatarModified(chr, MapPacket.AvatarModFlag.Speed);
            }
        }

        private static void StopSkill(Character chr, int skillid)
        {
            if (chr.PrimaryStats.HasBuff(skillid) == false) return;

            chr.PrimaryStats.RemoveByReference(skillid);
            if (skillid == Constants.Rogue.Skills.DarkSight)
            {
                // Are we debuffing twice here?
                MapPacket.CancelSkillEffect(chr, skillid); //?
                MapPacket.SendPlayerSkillAnim(chr, skillid, 1);
                MapPacket.SendPlayerDebuffed(chr, BuffValueTypes.DarkSight);
            }
        }

        public static void HandleStopSkill(Character chr, Packet packet)
        {
            var skillid = packet.ReadInt();
            StopSkill(chr, skillid);
        }

        public static void HandleAddSkillLevel(Character chr, Packet packet)
        {
            var SkillID = packet.ReadInt(); // Todo, add check.

            if (chr.PrimaryStats.SP <= 0)
            {
                // No SP left...
                InventoryPacket.NoChange(chr);
                return;
            }

            if (!DataProvider.Skills.TryGetValue(SkillID, out var sd))
            {
                Program.MainForm.LogAppend("Character {0} tried to put points in a skill ({1}) that doesnt exist.", chr.ID, SkillID);
                return;
            }

            if (chr.Skills.Skills.TryGetValue(SkillID, out var skillLevel) &&
                skillLevel >= sd.MaxLevel)
            {
                // Reached max points, stop
                InventoryPacket.NoChange(chr);
                return;
            }

            if (sd.RequiredSkills != null)
            {
                foreach (var sdRequiredSkill in sd.RequiredSkills)
                {
                    if (chr.Skills.GetSkillLevel(sdRequiredSkill.Key) < sdRequiredSkill.Value)
                    {
                        Program.MainForm.LogAppend(
                            "Character {0} tried to put points in a skill ({1}) without having enough points in {2} (req {3})",
                            chr.ID, SkillID, sdRequiredSkill.Key, sdRequiredSkill.Value);
                        InventoryPacket.NoChange(chr);
                        return;
                    }
                }
            }

            var jobOfSkill = Constants.getSkillJob(SkillID);
            var jobTrackOfSkill = Constants.getJobTrack(jobOfSkill);

            // Check if the user tried to get a skill from a different job
            if (
                Constants.getJobTrack(chr.PrimaryStats.Job) != jobTrackOfSkill ||
                jobOfSkill > chr.PrimaryStats.Job
            )
            {
                Program.MainForm.LogAppend("Character {0} tried to put points in a skill ({1}) for the wrong job.", chr.ID, SkillID);
                // More hax
                return;
            }

            chr.Skills.AddSkillPoint(SkillID);
            chr.AddSP(-1);
        }

        public static void SendAddSkillPoint(Character chr, int skillid, byte level)
        {
            var pw = new Packet(ServerMessages.CHANGE_SKILL_RECORD_RESULT);
            pw.WriteByte(0x01);
            pw.WriteShort(1);
            pw.WriteInt(skillid);
            pw.WriteInt(level);
            pw.WriteByte(1);

            chr.SendPacket(pw);
        }

        public static void SendSetSkillPoints(Character chr, Dictionary<int, byte> skills)
        {
            var pw = new Packet(ServerMessages.CHANGE_SKILL_RECORD_RESULT);
            pw.WriteByte(0x01);
            pw.WriteShort((short)skills.Count);
            foreach (var skill in skills)
            {
                pw.WriteInt(skill.Key);
                pw.WriteInt(skill.Value);
            }
            pw.WriteByte(1);

            chr.SendPacket(pw);
        }

        public static void HandlePrepareSkill(Character chr, Packet pw)
        {
            var SkillID = pw.ReadInt();
            var SLV = pw.ReadByte();
            var Action = pw.ReadByte();
            var ActionSpeed = pw.ReadByte();
            BroadcastSkillPrepare(chr, SkillID, SLV, Action, ActionSpeed);
        }

        public static void BroadcastSkillPrepare(Character chr, int skillId, byte slv, byte action, byte actionspeed)
        {
            var pw = new Packet(ServerMessages.PREPARE_SKILL);
            pw.WriteInt(chr.ID);
            pw.WriteInt(skillId);
            pw.WriteByte(slv);
            pw.WriteByte(action);
            pw.WriteByte(actionspeed);

            chr.Field.SendPacket(pw, chr);
        }
    }
}