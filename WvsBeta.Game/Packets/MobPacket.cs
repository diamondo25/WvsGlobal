using System;
using System.Collections.Generic;
using System.Linq;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;
using WvsBeta.Game.Packets;

namespace WvsBeta.Game
{
    public static class MobPacket
    {
        public static void HandleMobControl(Character victim, Packet packet)
        {
            int mobid = packet.ReadInt();
            var mob = victim.Field.GetMob(mobid);
            

            short moveID = packet.ReadShort();
            var x = packet.ReadByte();
            bool bNextAttackPossible = (x & 0x0F) != 0;
            sbyte action = packet.ReadSByte();

            int actualAction = action < 0 ? -1 : (byte)(action >> 1);

            uint dwData = packet.ReadUInt();


            var movePath = new MovePath();
            movePath.DecodeFromPacket(packet, MovePath.MovementSource.Mob);

            if (mob == null) return;

            if (mob.Controller != victim &&
                (!bNextAttackPossible || mob.NextAttackPossible || !mob.Field.FindNewController(mob, victim, true)))
            {
                SendMobRequestEndControl(victim, mobid);
                return;
            }


            victim.TryTraceMovement(movePath);

            if (mob.Controller != null && victim.ID != mob.Controller.ID) { Program.MainForm.LogAppend("returning mobpacket"); return; }

            var lastMoveMillis = MasterThread.CurrentTime - mob.LastMove;
            bool justStartedControlling = (MasterThread.CurrentTime - mob.LastControllerAssignTime) < 2000;
            
            PacketHelper.ValidateMovePath(mob, movePath);

            //Program.MainForm.LogDebug("[" + DateTime.Now.ToString() + "]" + "Received movement packet from " + victim.Name + ". Original pos: x:" + movePath.OriginalPosition.X + " y: " + movePath.OriginalPosition.Y + "\r\nNew position: x: " + movePath.NewPosition.X + " y: " + movePath.NewPosition.Y);


            // Skill related?
            if (actualAction >= 21 && actualAction <= 25)
            {
                short attackDelay = (short)(dwData >> 16);
                byte level = (byte)(dwData >> 8);
                byte skillId = (byte)(dwData);

                if (mob.DoSkill(skillId, level, attackDelay) == false)
                {
                    // invalid
                    return;
                }
            }
            else if (actualAction > 12 && actualAction < 20)
            {
                // regular attack?
                var attackIdx = (byte)(actualAction - 12);
                if (mob.Data.Attacks == null ||
                    !mob.Data.Attacks.ContainsKey(attackIdx))
                {
                    Program.MainForm.LogAppend(
                        "Unknown attack for mob: " + mob.MobID + "; " + attackIdx + "; " + packet);
                }
                else
                {
                    var attack = mob.Data.Attacks[attackIdx];
                    mob.MP = Math.Max(0, mob.MP - attack.MPConsume);
                    mob.LastAttack = MasterThread.CurrentTime;
                }
            }

            
            if ((MasterThread.CurrentTime - mob.LastAttack) > 5000)
            {
                // Reassign controller!
                MobDamageInfo lastHitCharacter;
                if (mob.IsControlled)
                {
                    var currentControllerID = mob.Controller.ID;
                    lastHitCharacter = mob.DamageLog.Log.FirstOrDefault(mobDamageInfo =>
                    {
                        if (mobDamageInfo.CharacterID == currentControllerID) return false;
                        if (mob.Field.GetPlayer(mobDamageInfo.CharacterID) == null) return false;

                        return (MasterThread.CurrentDate - mobDamageInfo.Time).TotalSeconds <= 5.0;
                    });
                }
                else
                {
                    lastHitCharacter = mob.DamageLog.Log.FirstOrDefault(mobDamageInfo =>
                    {
                        if (mob.Field.GetPlayer(mobDamageInfo.CharacterID) == null) return false;

                        return (MasterThread.CurrentDate - mobDamageInfo.Time).TotalSeconds <= 5.0;
                    });
                }

                if (lastHitCharacter != null)
                {
                    Program.MainForm.LogDebug("Setting new controller: " + Server.Instance.GetCharacter(lastHitCharacter.CharacterID));
                    mob.SetController(mob.Field.GetPlayer(lastHitCharacter.CharacterID), true, false);
                    return;
                }

            }


            mob.NextAttackPossible = bNextAttackPossible;

            // Prepare next skill
            byte forceControllerSkillLevel = 0;
            if (mob.NextAttackPossible == false ||
                mob.SkillCommand != 0 ||
                (mob.HasAnyStatus && mob.Status.BuffSealSkill.IsSet()) ||
                mob.Data.Skills == null ||
                mob.Data.Skills.Count == 0 ||
                (MasterThread.CurrentTime - mob.LastSkillUse) < 3000)
            {
                // No skill
            }
            else
            {
                var availableSkills = mob.Data.Skills.Where(skill =>
                {
                    Dictionary<byte, MobSkillLevelData> msdLevels;
                    if (!DataProvider.MobSkills.TryGetValue(skill.SkillID, out msdLevels) ||
                        !msdLevels.ContainsKey(skill.Level)) return false;

                    // Handle HP restriction
                    var msd = msdLevels[skill.Level];
                    if (msd.HPLimit > 0 && (mob.HP / (double)mob.MaxHP * 100.0) > msd.HPLimit)
                        return false;

                    // Skip if we already used a skill and it was not yet cooled down
                    if (mob.SkillsInUse.TryGetValue(msd.SkillID, out long lastUse) &&
                        (lastUse + (msd.Cooldown * 1000)) > MasterThread.CurrentTime)
                        return false;

                    // Do not reach the summon limit
                    if (skill.SkillID == (byte)Constants.MobSkills.Skills.Summon &&
                        mob.SummonCount + msd.Summons.Count > msd.SummonLimit) return false;


                    // Can we boost stats?
                    if (mob.HasAnyStatus)
                    {
                        short currentX = 0;
                        int maxX = Math.Abs(100 - msd.X);

                        switch ((Constants.MobSkills.Skills)skill.SkillID)
                        {
                            case Constants.MobSkills.Skills.WeaponAttackUp:
                            case Constants.MobSkills.Skills.WeaponAttackUpAoe:
                                currentX = mob.Status.BuffPowerUp.N;
                                break;
                            case Constants.MobSkills.Skills.MagicAttackUp:
                            case Constants.MobSkills.Skills.MagicAttackUpAoe:
                                currentX = mob.Status.BuffMagicUp.N;
                                break;
                            case Constants.MobSkills.Skills.WeaponDefenseUp:
                            case Constants.MobSkills.Skills.WeaponDefenseUpAoe:
                                currentX = mob.Status.BuffPowerGuardUp.N;
                                break;
                            case Constants.MobSkills.Skills.MagicDefenseUp:
                            case Constants.MobSkills.Skills.MagicDefenseUpAoe:
                                currentX = mob.Status.BuffMagicGuardUp.N;
                                break;
                        }

                        if (currentX == 0) return true;

                        if (Math.Abs(100 - currentX) >= maxX) return false;

                    }
                    return true;
                }).ToArray();

                if (availableSkills.Length > 0)
                {
                    var randomSkill = availableSkills[Rand32.Next() % availableSkills.Length];
                    mob.SkillCommand = randomSkill.SkillID;
                    forceControllerSkillLevel = randomSkill.Level;
                }
            }

            byte forceControllerSkillID = mob.SkillCommand;
            // Fix crash (zero level skill)
            if (forceControllerSkillLevel == 0)
                forceControllerSkillID = 0;

            SendMobControlResponse(victim, mobid, moveID, bNextAttackPossible, (short)mob.MP, forceControllerSkillID, forceControllerSkillLevel);

            SendMobControlMove(victim, mob, bNextAttackPossible, (byte)action, dwData, movePath);
            
            mob.CheckVacHack(lastMoveMillis, movePath.OriginalPosition, movePath.NewPosition, victim);

            // Good luck on getting less.
            if (lastMoveMillis < 500 && !justStartedControlling && !victim.IsAFK)
            {
                if (victim.AssertForHack(mob.HackReportCounter++ > 5,
                    $"Movement speed too high! {lastMoveMillis}ms since last movement."))
                {
                    mob.HackReportCounter = 0;
                }
            }
        }

        public static void HandleDistanceFromBoss(Character chr, Packet packet)
        {
            int mapmobid = packet.ReadInt();
            int distance = packet.ReadInt();
            // Do something with it :P
        }

        private static void MobData(Packet pw, Mob mob)
        {
            pw.WriteInt(mob.SpawnID);
            pw.WriteInt(mob.MobID);
            pw.WriteShort(mob.Position.X);
            pw.WriteShort(mob.Position.Y);
            
            byte bitfield = (byte)(mob.Owner != null ? 0x08 : 0x02);
            if (!mob.IsFacingRight()) bitfield |= 0x01;
            if (mob.Data.Flies) bitfield |= 0x04;
            
            pw.WriteByte(bitfield); // Bitfield
            pw.WriteShort(mob.Foothold);
            pw.WriteShort(mob.OriginalFoothold); // Original foothold, doesn't really matter
            
            pw.WriteSByte(mob.SummonType);
            if (mob.SummonType == -3 || mob.SummonType >= 0)
                pw.WriteInt(mob.SummonOption);

            if (mob.HasAnyStatus)
                mob.Status.Encode(pw, MobStatus.MobStatValue.ALL);
            else
                pw.WriteInt(0);
        }

        public static void SendMobSpawn(Character victim, Mob mob)
        {
            var pw = new Packet(ServerMessages.MOB_ENTER_FIELD);
            MobData(pw, mob);

            victim.SendPacket(pw);
        }

        public static void SendMobSpawn(Mob mob)
        {
            var pw = new Packet(ServerMessages.MOB_ENTER_FIELD);
            MobData(pw, mob);

            mob.Field.SendPacket(mob, pw);
        }

        public static void SendMobDeath(Mob mob, byte how)
        {
            var pw = new Packet(ServerMessages.MOB_LEAVE_FIELD);
            pw.WriteInt(mob.SpawnID);
            pw.WriteByte(how);
            mob.Field.SendPacket(mob, pw);
        }

        public static void SendMobRequestControl(Character currentController, Mob mob, bool chasing)
        {
            var pw = new Packet(ServerMessages.MOB_CHANGE_CONTROLLER);
            pw.WriteByte((byte)(chasing ? 2 : 1));
            MobData(pw, mob);

            currentController.SendPacket(pw);
        }

        public static void SendMobRequestEndControl(Character currentController, int spawnId)
        {
            var pw = new Packet(ServerMessages.MOB_CHANGE_CONTROLLER);
            pw.WriteByte(0);
            pw.WriteInt(spawnId);
            currentController.SendPacket(pw);
        }

        public static void SendMobControlResponse(Character victim, int mobid, short moveid, bool bNextAttackPossible, short MP, byte skillCommand, byte level)
        {
            var pw = new Packet(ServerMessages.MOB_MOVE_RESPONSE);
            pw.WriteInt(mobid);
            pw.WriteShort(moveid);
            pw.WriteBool(bNextAttackPossible);
            pw.WriteShort(MP);
            pw.WriteByte(skillCommand);
            pw.WriteByte(level);

            victim.SendPacket(pw);
        }

        public static void SendMobControlMove(Character victim, Mob mob, bool bNextAttackPossible, byte action, uint dwData, MovePath movePath)
        {
            var pw = new Packet(ServerMessages.MOB_MOVE);
            pw.WriteInt(mob.SpawnID);
            pw.WriteBool(bNextAttackPossible);
            pw.WriteByte(action);
            pw.WriteUInt(dwData); // Unknown

            movePath.EncodeToPacket(pw);

            victim.Field.SendPacket(mob, pw, victim);
        }

        public static void SendMobDamageOrHeal(Character victim, int spawnId, int amount, bool isHeal, bool web)
        {
            Packet pw = new Packet(ServerMessages.MOB_DAMAGED);
            pw.WriteInt(spawnId);
            pw.WriteBool(!web); // 0 = caused by web, 1 = caused by obstacle, heal, skill or more?
            pw.WriteInt((isHeal ? -amount : amount));
            pw.WriteLong(0);
            pw.WriteLong(0);
            victim.Field.SendPacket(pw, victim);
        }

        public static void SendMobDamageOrHeal(Map field, Mob mob, int amount, bool isHeal, bool web)
        {
            Packet pw = new Packet(ServerMessages.MOB_DAMAGED);
            pw.WriteInt(mob.SpawnID);
            pw.WriteBool(!web); // 0 = caused by web, 1 = caused by obstacle, heal, skill or more?
            pw.WriteInt((isHeal ? -amount : amount));
            // if damagedByMob mob, write 2 ints (HP, Max HP). Not sure if this version contains this

            pw.WriteLong(0);
            pw.WriteLong(0);
            field.SendPacket(mob, pw);
        }

        public static void SendMobStatsTempSet(Mob pMob, short pDelay, MobStatus.MobStatValue pSpecificFlag = MobStatus.MobStatValue.ALL)
        {
            Packet pw = new Packet(ServerMessages.MOB_STAT_SET);
            pw.WriteInt(pMob.SpawnID);
            if (pMob.HasAnyStatus)
                pMob.Status.Encode(pw, pSpecificFlag);
            else
                pw.WriteInt(0);
            pw.WriteShort(pDelay);

            pMob.Field.SendPacket(pMob, pw);
        }

        public static void SendMobStatsTempReset(Mob pMob, MobStatus.MobStatValue pFlags)
        {
            if (pFlags == 0) return;
            Packet pw = new Packet(ServerMessages.MOB_STAT_RESET);
            pw.WriteInt(pMob.SpawnID);

            pw.WriteUInt((uint)pFlags);

            pMob.Field.SendPacket(pMob, pw);
        }
    }
}