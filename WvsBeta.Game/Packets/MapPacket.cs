using System;
using System.Diagnostics;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;
using WvsBeta.Game.Events;
using WvsBeta.Game.Events.PartyQuests;
using WvsBeta.Game.GameObjects;
using WvsBeta.Game.Packets;

namespace WvsBeta.Game
{
    public static class MapPacket
    {
        [Flags]
        public enum AvatarModFlag
        {
            Skin = 1,
            Face = 2,
            Equips = 4,
            ItemEffects = 8 | 0x10,
            Speed = 0x10,
            Rings = 0x20
        }

        public static void HandleMove(Character chr, Packet packet)
        {
            if (packet.ReadByte() != chr.PortalCount) return;

            var movePath = new MovePath();
            movePath.DecodeFromPacket(packet, MovePath.MovementSource.Player);
            chr.TryTraceMovement(movePath);

            if (chr.AssertForHack(movePath.Elements.Length == 0, "Received Empty Move Path"))
            {
                return;
            }

            bool allowed = PacketHelper.ValidateMovePath(chr, movePath);
            if (!allowed && !chr.IsGM)
            {
                //this.Session.Socket.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                //return;
                // TODO: Update speed of character
                // Program.MainForm.LogAppendFormat("Move incorrect: {0}", chr.Name);
            }
            SendPlayerMove(chr, movePath);

            if (!chr.Field.ReallyOutOfBounds.Contains(chr.Position.X, chr.Position.Y))
            {
                if (chr.OutOfMBRCount++ > 5)
                {
                    // Okay, reset.
                    chr.ChangeMap(chr.MapID, chr.Field.GetClosestStartPoint(chr.Position));
                    chr.OutOfMBRCount = 0;
                }
            }
            else
            {
                chr.OutOfMBRCount = 0;
            }
        }

        public static void OnContiMoveState(Character chr, Packet packet)
        {
            int mapid = packet.ReadInt();

            var p = new Packet(ServerMessages.CONTISTATE);
            p.WriteByte((byte)ContinentMan.Instance.GetInfo(mapid, 0));
            p.WriteByte((byte)ContinentMan.Instance.GetInfo(mapid, 1));
            chr.SendPacket(p);
        }

        public static void HandleNPCChat(Character chr, Packet packet)
        {
            int npcId = packet.ReadInt();
            var Npc = chr.Field.GetNPC(npcId);

            if (chr.AssertForHack(!chr.CanAttachAdditionalProcess, "Tried to chat to npc while not able to attach additional process"))
            {
                InventoryPacket.NoChange(chr);
                return;
            }

            // Npc doesnt exist
            if (Npc == null)
            {
                InventoryPacket.NoChange(chr);
                return;
            }

            int RealID = Npc.ID;
            if (!DataProvider.NPCs.TryGetValue(RealID, out NPCData npc)) return;

            if (npc.Shop.Count > 0)
            {
                // It's a shop!
                chr.ShopNPCID = RealID;
                NpcPacket.SendShowNPCShop(chr, RealID);
            }
            else if (npc.Trunk > 0)
            {
                chr.TrunkNPCID = RealID;
                StoragePacket.SendShowStorage(chr, chr.TrunkNPCID);
            }
            else
            {
                Action<string> errorHandlerFnc = null;
                if (chr.IsGM)
                {
                    errorHandlerFnc = (script) =>
                    {
                        MessagePacket.SendNotice("Error compiling script '" + script + "'!", chr);
                    };
                }

                INpcScript NPC = null;
                if (NPC == null && npc.Quest != null) NPC = Server.Instance.TryGetOrCompileScript(npc.Quest, errorHandlerFnc);
                if (NPC == null) NPC = Server.Instance.TryGetOrCompileScript(npc.ID.ToString(), errorHandlerFnc);

                NpcChatSession.Start(RealID, NPC, chr);
            }
        }

        public static void OnEnterPortal(Packet packet, Character chr)
        {
            if (packet.ReadByte() != chr.PortalCount)
            {
                InventoryPacket.NoChange(chr);
                return;
            }

            int opcode = packet.ReadInt();
            string portalname = packet.ReadString();
            if (portalname.Length > 0)
            {
                new Pos(packet);
            }
            packet.ReadByte(); // Related to teleporting to party member? Always 0
            packet.ReadByte(); // unk

            switch (opcode)
            {
                case 0:
                    {
                        if (chr.PrimaryStats.HP == 0)
                        {
                            chr.HandleDeath();
                        }
                        else if (!chr.IsGM)
                        {
                            Program.MainForm.LogAppend($"Not handling death of {chr.ID}, because user is not dead. Killing him again. HP: " + chr.PrimaryStats.HP);
                            // Kill him anyway
                            chr.DamageHP(30000);
                        }
                        else
                        {
                            // Admin /map 0
                            chr.ChangeMap(opcode);
                        }
                        break;
                    }
                case -1:
                    {

                        if (chr.Field.Portals.TryGetValue(portalname, out Portal portal) &&
                            DataProvider.Maps.TryGetValue(portal.ToMapID, out Map toMap) &&
                            toMap.Portals.TryGetValue(portal.ToName, out Portal to))
                        {
                            var pos = new Pos(portal.X, portal.Y);
                            var dist = chr.Position - pos;
                            if (chr.AssertForHack(dist > 300, "Portal distance hack (" + dist + ")", dist > 600))
                            {
                                InventoryPacket.NoChange(chr);
                                return;
                            }

                            if (portal.Enabled == false)
                            {
                                Program.MainForm.LogDebug(chr.Name + " tried to enter a disabled portal.");
                                BlockedMessage(chr, PortalBlockedMessage.ClosedForNow);
                                InventoryPacket.NoChange(chr);
                                return;
                            }

                            if (chr.Field.PortalsOpen == false)
                            {
                                Program.MainForm.LogDebug(chr.Name + " tried to enter a disabled portal.");
                                BlockedMessage(chr, PortalBlockedMessage.ClosedForNow);
                                InventoryPacket.NoChange(chr);
                            }
                            else if (chr.Field.PQPortalOpen)
                            {
                                chr.ChangeMap(portal.ToMapID, to);
                            }
                            else
                            {
                                BlockedMessage(chr, PortalBlockedMessage.CannotGoToThatPlace);
                            }

                        }
                        else
                        {
                            Program.MainForm.LogDebug(chr.Name + " tried to enter unknown portal??? " + portalname + ", " + chr.Field.ID);
                            BlockedMessage(chr, PortalBlockedMessage.ClosedForNow);
                        }


                        break;
                    }
                default:
                    {
                        if (chr.IsGM)
                        {
                            chr.ChangeMap(opcode);
                        }
                        break;
                    }
            }
        }

        public static void HandleSitChair(Character chr, Packet packet)
        {
            short chair = packet.ReadShort();

            if (chair == -1)
            {
                if (chr.MapChair != -1)
                {
                    chr.Field.UsedSeats.Remove(chr.MapChair);
                    chr.MapChair = -1;
                    SendCharacterSit(chr, -1);
                }
                else
                {
                    InventoryPacket.NoChange(chr);
                }
            }
            else
            {
                if (chr.Field != null && chr.Field.Seats.ContainsKey(chair) && !chr.Field.UsedSeats.Contains(chair))
                {
                    chr.Field.UsedSeats.Add(chair);
                    chr.MapChair = chair;
                    SendCharacterSit(chr, chair);
                }
                else
                {
                    InventoryPacket.NoChange(chr);
                }
            }
        }

        public static void ShowNPC(NpcLife npcLife, Character victim)
        {
            Packet pw;/* = new Packet(ServerMessages.NPC_ENTER_FIELD);
            pw.WriteUInt(npcLife.SpawnID);
            pw.WriteInt(npcLife.ID);
            pw.WriteShort(npcLife.X);
            pw.WriteShort(npcLife.Y);
            pw.WriteBool(!npcLife.FacesLeft);
            pw.WriteUShort(npcLife.Foothold);
            pw.WriteShort(npcLife.Rx0);
            pw.WriteShort(npcLife.Rx1);

            victim.sendPacket(pw);
            */

            pw = new Packet(ServerMessages.NPC_CHANGE_CONTROLLER);
            pw.WriteBool(true);
            pw.WriteUInt(npcLife.SpawnID);
            pw.WriteInt(npcLife.ID);
            pw.WriteShort(npcLife.X);
            pw.WriteShort(npcLife.Y);
            pw.WriteBool(!npcLife.FacesLeft);
            pw.WriteUShort(npcLife.Foothold);
            pw.WriteShort(npcLife.Rx0);
            pw.WriteShort(npcLife.Rx1);

            victim.SendPacket(pw);
        }



        public static void HandleNPCAnimation(Character controller, Packet packet)
        {
            Packet pw = new Packet(ServerMessages.NPC_ANIMATE);
            pw.WriteBytes(packet.ReadLeftoverBytes());

            controller.SendPacket(pw);
        }

        public static void SendWeatherEffect(Map map, Character victim = null)
        {
            Packet pw = new Packet(ServerMessages.BLOW_WEATHER);
            pw.WriteBool(map.WeatherIsAdmin);
            pw.WriteInt(map.WeatherID);
            if (!map.WeatherIsAdmin)
                pw.WriteString(map.WeatherMessage);

            if (victim != null)
                victim.SendPacket(pw);
            else
                map.SendPacket(pw);
        }

        public static void SendPlayerMove(Character chr, MovePath movePath)
        {
            Packet pw = new Packet(ServerMessages.MOVE_PLAYER);
            pw.WriteInt(chr.ID);
            movePath.EncodeToPacket(pw);

            chr.Field.SendPacket(chr, pw, chr);
        }

        public static void SendChatMessage(Character who, string message)
        {
            Packet pw = new Packet(ServerMessages.CHAT);
            pw.WriteInt(who.ID);
            pw.WriteBool(who.IsGM && !who.Undercover);
            pw.WriteString(message);

            who.Field.SendPacket(who, pw);
        }

        public static void SendEmotion(Character chr, int emotion)
        {
            Packet pw = new Packet(ServerMessages.FACIAL_EXPRESSION);
            pw.WriteInt(chr.ID);
            pw.WriteInt(emotion);

            chr.Field.SendPacket(chr, pw, chr);
        }

        public static void SendCharacterLeavePacket(Character who)
        {
            Packet pw = new Packet(ServerMessages.USER_LEAVE_FIELD);
            pw.WriteInt(who.ID);
            who.Field.SendPacket(who, pw, who);
        }

        public static void SendCharacterLeavePacket(int id, Character victim)
        {
            Packet pw = new Packet(ServerMessages.USER_LEAVE_FIELD);
            pw.WriteInt(id);
            victim.SendPacket(pw);
        }

        public static void SendCharacterSit(Character chr, short chairid)
        {
            Packet pw = new Packet(ServerMessages.SHOW_CHAIR);
            pw.WriteBool(chairid != -1);
            if (chairid != -1)
            {
                pw.WriteShort(chairid);
            }
            chr.SendPacket(pw);
        }

        public static void SendBossHPBar(Map pField, int pHP, int pMaxHP, int pColorBottom, int pColorTop)
        {
            Packet pw = new Packet(ServerMessages.FIELD_EFFECT);
            pw.WriteByte(5);
            pw.WriteInt(pHP);
            pw.WriteInt(pMaxHP);
            pw.WriteInt(pColorTop);
            pw.WriteInt(pColorBottom);
            pField.SendPacket(pw);
        }

        public static void MapEffect(Character chr, byte type, string message, bool ToTeam)
        {
            //Sounds : Party1/Clear // Party1/Failed
            //Messages : quest/party/clear // quest/party/wrong_kor
            Packet pw = new Packet(ServerMessages.FIELD_EFFECT);
            pw.WriteByte(type); //4: sound 3: message
            pw.WriteString(message);
            if (!ToTeam)
            {
                chr.Field.SendPacket(pw);
            }
            else
            {
                chr.SendPacket(pw);
            }
        }

        public static void PortalEffect(Map field, byte what, string message)
        {

            Packet pw = new Packet(ServerMessages.FIELD_EFFECT);
            pw.WriteByte(2); //2
            pw.WriteByte(what); //?? Unknown 
            pw.WriteString(message); //gate
            field.SendPacket(pw);
        }

        public static void Kite(Character chr, Kite Kite)
        {
            Packet pw = new Packet(ServerMessages.MESSAGE_BOX_ENTER_FIELD);
            pw.WriteInt(Kite.ID);
            pw.WriteInt(Kite.ItemID);
            pw.WriteString(Kite.Message);
            pw.WriteString(chr.Name);
            pw.WriteShort(Kite.X);
            pw.WriteShort(Kite.Y); //Should be close enough :P
            chr.Field.SendPacket(Kite, pw);
        }

        public static void RemoveKite(Map Field, Kite Kite, byte LeaveType)
        {
            Packet pw = new Packet(ServerMessages.MESSAGE_BOX_LEAVE_FIELD);
            pw.WriteByte(LeaveType);
            pw.WriteInt(Kite.ID);
            Field.SendPacket(Kite, pw);
        }

        public static void KiteMessage(Character chr)
        {
            //Can't fly it here
            Packet pw = new Packet(ServerMessages.MESSAGE_BOX_CREATE_FAILED);
            pw.WriteByte(0);
            chr.SendPacket(pw);
        }

        public static void ShowMapTimerForCharacter(Character chr, int time)
        {
            Packet pw = new Packet(ServerMessages.CLOCK);
            pw.WriteByte(0x02);
            pw.WriteInt(time);
            chr.SendPacket(pw);
        }

        public static void ShowMapTimerForMap(Map map, int time)
        {
            Packet pw = new Packet(ServerMessages.CLOCK);
            pw.WriteByte(0x02);
            pw.WriteInt(time);
            map.SendPacket(pw);
        }

        public static void SendGMEventInstructions(Map map)
        {
            //Its in korean :S
            Packet pw = new Packet(ServerMessages.DESC); // Could be quiz, dont think so though..
            pw.WriteByte(0x00);
            map.SendPacket(pw);
        }

        public static void SendMapClock(Character chr, int hour, int minute, int second)
        {
            Packet pw = new Packet(ServerMessages.CLOCK);
            pw.WriteByte(0x01);
            pw.WriteByte((byte)hour);
            pw.WriteByte((byte)minute);
            pw.WriteByte((byte)second);
            chr.SendPacket(pw);
        }

        public static void SendJukebox(Map map, Character victim)
        {
            Packet pw = new Packet(ServerMessages.PLAY_JUKE_BOX);
            pw.WriteInt(map.JukeboxID);
            if (map.JukeboxID != -1)
                pw.WriteString(map.JukeboxUser);

            if (victim != null)
                victim.SendPacket(pw);
            else
                map.SendPacket(pw);
        }

        public enum PortalBlockedMessage
        {
            ClosedForNow = 1,
            CannotGoToThatPlace = 2
        }

        public static void BlockedMessage(Character chr, byte msg) => BlockedMessage(chr, (PortalBlockedMessage)msg);

        public static void BlockedMessage(Character chr, PortalBlockedMessage msg)
        {
            Packet pw = new Packet(ServerMessages.TRANSFER_FIELD_REQ_IGNORED);
            pw.WriteByte((byte)msg);
            chr.SendPacket(pw);
        }

        public static void SpawnPortal(Character chr, int srcMapId, int destMapId, short destX, short destY)
        {
            //spawns a portal (Spawnpoint in the map you are going to spawn in)
            Packet pw = new Packet(ServerMessages.TOWN_PORTAL);

            pw.WriteInt(destMapId);
            pw.WriteInt(srcMapId);
            pw.WriteShort(destX);
            pw.WriteShort(destY);
            chr.SendPacket(pw);
        }

        public static void SpawnPortalParty(Character chr, byte ownerIdIdx, int srcMapId, int destMapId, short destX, short destY)
        {
            Packet pw = new Packet(ServerMessages.PARTY_RESULT);
            pw.WriteByte(26); //door change
            pw.WriteByte(ownerIdIdx);
            pw.WriteInt(destMapId);
            pw.WriteInt(srcMapId);
            pw.WriteShort(destX);
            pw.WriteShort(destY);
            chr.SendPacket(pw);
        }

        public static void RemovePortal(Character chr)
        {
            Packet pw = new Packet(ServerMessages.TOWN_PORTAL);
            pw.WriteInt(Constants.InvalidMap);
            pw.WriteInt(Constants.InvalidMap);
            chr.SendPacket(pw);
        }

        public static void SendPinkText(Character chr, string text) //needs work 
        {
            Packet pw = new Packet(ServerMessages.GROUP_MESSAGE);
            pw.WriteByte(1);
            pw.WriteString(chr.Name);
            pw.WriteString(text);
            chr.SendPacket(pw);
        }

        public static void SendCharacterEnterPacket(Character player, Character victim)
        {
            Packet pw = new Packet(ServerMessages.USER_ENTER_FIELD);

            pw.WriteInt(player.ID);

            pw.WriteString(player.Name);

            BuffPacket.AddMapBuffValues(player, pw);

            PacketHelper.AddAvatar(pw, player);

            pw.WriteInt(player.GetSpawnedPet()?.ItemID ?? 0);
            pw.WriteInt(player.Inventory.ActiveItemID);
            pw.WriteInt(player.Inventory.ChocoCount);
            pw.WriteShort(player.Position.X);
            pw.WriteShort(player.Position.Y);
            pw.WriteByte(player.Stance);
            pw.WriteShort(player.Foothold);
            pw.WriteBool(player.IsGM && !player.Undercover);

            var petItem = player.GetSpawnedPet();
            pw.WriteBool(petItem != null);
            if (petItem != null)
            {
                pw.WriteInt(petItem.ItemID);
                pw.WriteString(petItem.Name);
                pw.WriteLong(petItem.CashId);
                var ml = petItem.MovableLife;
                pw.WriteShort(ml.Position.X);
                pw.WriteShort(ml.Position.Y);
                pw.WriteByte(ml.Stance);
                pw.WriteShort(ml.Foothold);
            }

            // Mini Game & Player Shops
            pw.WriteByte(0); // Hardcoded end of minigame & player shops until implemented

            //Rings
            pw.WriteByte(0); // Number of Rings, hardcoded 0 until implemented.

            //Ring packet structure
            /**
            for (Ring ring in player.Rings()) {
                pw.WriteLong(ring.getRingId()); // R
                pw.WriteLong(ring.getPartnerRingId());
                pw.WriteInt(ring.getItemId());
            }
            */
            victim.SendPacket(pw);
        }

        public static void SendPlayerInfo(Character chr, Packet packet)
        {
            int id = packet.ReadInt();
            Character victim = chr.Field.GetPlayer(id);
            if (victim == null)
            {
                InventoryPacket.NoChange(chr);
                return;
            }

            Packet pw = new Packet(ServerMessages.CHARACTER_INFO); // Idk why this is in mappacket, it's part of CWvsContext
            pw.WriteInt(victim.ID);
            pw.WriteByte(victim.PrimaryStats.Level);
            pw.WriteShort(victim.PrimaryStats.Job);
            pw.WriteShort(victim.PrimaryStats.Fame);

            if (chr.IsGM && !victim.IsGM)
                pw.WriteString("" + id + ":" + victim.UserID);
            else if (victim.IsGM && !victim.Undercover)
                pw.WriteString("Administrator");
            else
                pw.WriteString("");

            var petItem = victim.GetSpawnedPet();
            pw.WriteBool(petItem != null);
            if (petItem != null)
            {
                pw.WriteInt(petItem.ItemID);
                pw.WriteString(petItem.Name);
                pw.WriteByte(petItem.Level);
                pw.WriteShort(petItem.Closeness);
                pw.WriteByte(petItem.Fullness);
                pw.WriteInt(victim.Inventory.GetEquippedItemId((short)Constants.EquipSlots.Slots.PetEquip1, true)); // Pet equip.
            }

            pw.WriteByte((byte)victim.Wishlist.Count);
            victim.Wishlist.ForEach(pw.WriteInt);

            //todo : rings
            pw.WriteLong(0);

            chr.SendPacket(pw);
        }

        public static void SendAvatarModified(Character chr, AvatarModFlag AvatarModFlag = 0)
        {
            Packet pw = new Packet(ServerMessages.AVATAR_MODIFIED);
            pw.WriteInt(chr.ID);
            pw.WriteInt((int)AvatarModFlag);

            if ((AvatarModFlag & AvatarModFlag.Skin) == AvatarModFlag.Skin)
                pw.WriteByte(chr.Skin);
            if ((AvatarModFlag & AvatarModFlag.Face) == AvatarModFlag.Face)
                pw.WriteInt(chr.Face);

            pw.WriteBool((AvatarModFlag & AvatarModFlag.Equips) == AvatarModFlag.Equips);
            if ((AvatarModFlag & AvatarModFlag.Equips) == AvatarModFlag.Equips)
            {
                pw.WriteByte(0); //My Hair is a Bird, Your Argument is Invalid
                pw.WriteInt(chr.Hair);
                chr.Inventory.GeneratePlayerPacket(pw);
                pw.WriteByte(0xFF); // Equips shown end
                pw.WriteInt(chr.Inventory.GetEquippedItemId((short)Constants.EquipSlots.Slots.Weapon, true));
                pw.WriteInt(chr.GetSpawnedPet()?.ItemID ?? 0);
            }

            pw.WriteBool((AvatarModFlag & AvatarModFlag.ItemEffects) == AvatarModFlag.ItemEffects);
            if ((AvatarModFlag & AvatarModFlag.ItemEffects) == AvatarModFlag.ItemEffects)
            {
                pw.WriteInt(chr.Inventory.ActiveItemID);
                pw.WriteInt(chr.Inventory.ChocoCount);
            }

            pw.WriteBool((AvatarModFlag & AvatarModFlag.Speed) == AvatarModFlag.Speed);
            if ((AvatarModFlag & AvatarModFlag.Speed) == AvatarModFlag.Speed)
                pw.WriteByte(chr.PrimaryStats.TotalSpeed);

            pw.WriteBool((AvatarModFlag & AvatarModFlag.Rings) == AvatarModFlag.Rings);
            if ((AvatarModFlag & AvatarModFlag.Rings) == AvatarModFlag.Rings)
            {
                pw.WriteLong(0);
                pw.WriteLong(0);
            }

            chr.Field.SendPacket(chr, pw, chr);
        }


        public static void SendPlayerLevelupAnim(Character chr)
        {
            Packet pw = new Packet(ServerMessages.SHOW_FOREIGN_EFFECT);
            pw.WriteInt(chr.ID);
            pw.WriteByte(0x00);

            chr.Field.SendPacket(chr, pw, chr);
        }


        public static void SendPlayerSkillAnim(Character chr, int skillid, byte level)
        {
            Packet pw = new Packet(ServerMessages.SHOW_FOREIGN_EFFECT);
            pw.WriteInt(chr.ID);
            pw.WriteByte(0x01);
            pw.WriteInt(skillid);
            pw.WriteByte(level);

            chr.Field.SendPacket(chr, pw);
        }

        public static void SendPlayerSkillAnimSelf(Character chr, int skillid, byte level)
        {
            Packet pw = new Packet(ServerMessages.PLAYER_EFFECT); // Not updated
            pw.WriteByte(0x01);
            pw.WriteInt(skillid);
            pw.WriteByte(level);

            chr.SendPacket(pw);
        }

        public static void SendPlayerSkillAnimThirdParty(Character chr, int skillid, byte level, bool party, bool self)
        {
            Packet pw;
            if (party && self)
            {
                pw = new Packet(ServerMessages.PLAYER_EFFECT);
            }
            else
            {
                pw = new Packet(ServerMessages.SHOW_FOREIGN_EFFECT);
                pw.WriteInt(chr.ID);
            }
            pw.WriteByte((byte)(party ? 0x02 : 0x01));
            pw.WriteInt(skillid);
            pw.WriteByte(level);
            if (self)
            {
                chr.SendPacket(pw);
            }
            else
            {
                chr.Field.SendPacket(chr, pw, chr);
            }
        }

        public static void SendPlayerBuffed(Character chr, BuffValueTypes pBuffs, short delay = 0)
        {
            Packet pw = new Packet(ServerMessages.GIVE_FOREIGN_BUFF);
            pw.WriteInt(chr.ID);
            BuffPacket.AddMapBuffValues(chr, pw, pBuffs);
            pw.WriteShort(delay); // the delay. usually 0, but is carried on through OnStatChangeByMobSkill / DoActiveSkill_(Admin/Party/Self)StatChange

            chr.Field.SendPacket(chr, pw, chr);
        }

        public static void SendPlayerDebuffed(Character chr, BuffValueTypes buffFlags)
        {
            Packet pw = new Packet(ServerMessages.RESET_FOREIGN_BUFF);
            pw.WriteInt(chr.ID);
            pw.WriteUInt((uint)buffFlags);

            chr.Field.SendPacket(chr, pw, chr);
        }

        public static void SendChangeMap(Character chr)
        {
            Packet pack = new Packet(ServerMessages.SET_FIELD);
            pack.WriteInt(Server.Instance.ID); // Channel ID
            pack.WriteByte(chr.PortalCount);
            pack.WriteBool(false); // Is not connecting
            pack.WriteInt(chr.MapID);
            pack.WriteByte(chr.MapPosition);
            pack.WriteShort(chr.PrimaryStats.HP);
            chr.SendPacket(pack);
        }

        public static void EmployeeEnterField(Character chr) //hired merchant :D
        {
            Packet pw = new Packet(0x83); //not the right opcode
            pw.WriteByte(chr.PortalCount);
            pw.WriteInt(chr.ID);
            pw.WriteByte(0); //??
            pw.WriteInt(chr.MapID);
            pw.WriteInt(295); //Swaglord's ID
            pw.WriteByte(chr.MapPosition); //probably spawnpoint 
            pw.WriteShort(chr.Position.X);
            pw.WriteShort(chr.Position.Y);
            pw.WriteInt(1); //??
            pw.WriteShort(chr.PrimaryStats.HP);
            pw.WriteShort(chr.PrimaryStats.MP);
            pw.WriteShort(1); //??
            pw.WriteLong(0);
            pw.WriteLong(0);
            chr.SendPacket(pw);


        }

        public static void SendJoinGame(Character chr)
        {
            Packet pack = new Packet(ServerMessages.SET_FIELD);

            pack.WriteInt(Server.Instance.ID); // Channel ID
            pack.WriteByte(0); // 0 portals
            pack.WriteBool(true); // Is connecting

            {
                var rnd = Server.Instance.Randomizer;
                // Seeds are initialized by global randomizer
                var seed1 = rnd.Random();
                var seed2 = rnd.Random();
                var seed3 = rnd.Random();
                var seed4 = rnd.Random();

                chr.CalcDamageRandomizer.SetSeed(seed1, seed2, seed3);
                chr.RndActionRandomizer.SetSeed(seed2, seed3, seed4);

                pack.WriteUInt(seed1);
                pack.WriteUInt(seed2);
                pack.WriteUInt(seed3);
                pack.WriteUInt(seed4);
            }

            pack.WriteShort(-1); // Flags (contains everything: 0xFFFF)

            pack.WriteInt(chr.ID);
            pack.WriteString(chr.Name, 13);
            pack.WriteByte(chr.Gender); // Gender
            pack.WriteByte(chr.Skin); // Skin
            pack.WriteInt(chr.Face); // Face
            pack.WriteInt(chr.Hair); // Hair

            pack.WriteLong(chr.PetCashId); // Pet Cash ID :/

            pack.WriteByte(chr.PrimaryStats.Level);
            pack.WriteShort(chr.PrimaryStats.Job);
            pack.WriteShort(chr.PrimaryStats.Str);
            pack.WriteShort(chr.PrimaryStats.Dex);
            pack.WriteShort(chr.PrimaryStats.Int);
            pack.WriteShort(chr.PrimaryStats.Luk);
            pack.WriteShort(chr.PrimaryStats.HP);
            pack.WriteShort(chr.PrimaryStats.GetMaxHP(true));
            pack.WriteShort(chr.PrimaryStats.MP);
            pack.WriteShort(chr.PrimaryStats.GetMaxMP(true));
            pack.WriteShort(chr.PrimaryStats.AP);
            pack.WriteShort(chr.PrimaryStats.SP);
            pack.WriteInt(chr.PrimaryStats.EXP);
            pack.WriteShort(chr.PrimaryStats.Fame);

            pack.WriteInt(chr.MapID); // Mapid
            pack.WriteByte(chr.MapPosition); // Mappos

            pack.WriteLong(0);
            pack.WriteInt(0);
            pack.WriteInt(0);

            pack.WriteByte((byte)chr.PrimaryStats.BuddyListCapacity); // Buddylist slots

            chr.Inventory.GenerateInventoryPacket(pack);

            chr.Skills.AddSkills(pack);


            var questsWithData = chr.Quests.Quests;
            pack.WriteShort((short)questsWithData.Count); // Running quests
            foreach (var kvp in questsWithData)
            {
                pack.WriteInt(kvp.Key);
                pack.WriteString(kvp.Value.Data);
            }

            pack.WriteShort(0); // RPS Game(s)
                                /*
                                 * For every game stat:
                                 * pack.WriteInt(); // All unknown values
                                 * pack.WriteInt();
                                 * pack.WriteInt();
                                 * pack.WriteInt();
                                 * pack.WriteInt();
                                */


            pack.WriteShort(0);
            /*
             * For every ring, 33 unkown bytes.
            */


            chr.Inventory.AddRockPacket(pack);


            //pack.WriteByte(1); THIS IS WHAT TRIGGERS OMOK AND 5 LINES BELOW
            //pack.WriteInt(1112001);
            //pack.WriteInt(1112001);
            //pack.WriteInt(327);
            //pack.WriteInt(1112001);
            //pack.WriteInt(1112001);
            chr.SendPacket(pack);
        }

        public static void CancelSkillEffect(Character chr, int skillid)
        {
            Packet pw = new Packet(ServerMessages.SKILL_END);
            pw.WriteInt(chr.ID);
            pw.WriteInt(skillid);
            chr.Field.SendPacket(pw, chr);
        }

        public static Packet ShowDoor(MysticDoor door, byte enterType)
        {
            Packet pw = new Packet(ServerMessages.TOWN_PORTAL_CREATED);
            pw.WriteByte(enterType); //Does this decide if the animation plays when it is shown?
            pw.WriteInt(door.OwnerId);
            pw.WriteShort(door.X);
            pw.WriteShort(door.Y);

            Trace.WriteLine($"Spawning Door @ {door.X} {door.Y}, owner {door.OwnerId}");

            return pw;
        }

        public static Packet RemoveDoor(MysticDoor door, byte leaveType)
        {
            Packet pw = new Packet(ServerMessages.TOWN_PORTAL_REMOVED);
            pw.WriteByte(leaveType);
            pw.WriteInt(door.OwnerId);
            return pw;
        }

        public static void HandleDoorUse(Character chr, Packet packet)
        {
            int charid = packet.ReadInt();
            Program.MainForm.LogDebug("cid: " + charid);
            bool enterFromTown = packet.ReadBool();
            if (enterFromTown)
            {
                // When you enter from town and go to a training map
                // Resulting map is _not_ a town
                if (chr.Field.DoorPool.DoorsLeadingHere.TryGetValue(charid, out var door) && door.CanEnterDoor(chr))
                {
                    chr.ChangeMap(door.FieldId, PartyData.GetMemberIdx(charid) ?? 0, door);
                    return;
                }
            }
            else
            {
                // When you enter from a training map
                // Resulting map _is_ a town
                if (chr.Field.DoorPool.TryGetDoor(charid, out var door) && door.CanEnterDoor(chr))
                {
                    chr.ChangeMap(chr.Field.ReturnMap, PartyData.GetMemberIdx(charid) ?? 0, door);
                    return;
                }
            }

            InventoryPacket.NoChange(chr);
        }

        public static Packet ShowSummon(Summon summon, byte enterType)
        {
            Packet pw = new Packet(ServerMessages.SPAWN_ENTER_FIELD);
            pw.WriteInt(summon.OwnerId);
            pw.WriteInt(summon.SkillId);
            pw.WriteByte(summon.SkillLevel);
            pw.WriteShort(summon.Position.X);
            pw.WriteShort(summon.Position.Y);
            pw.WriteBool(summon.MoveAction);
            pw.WriteUShort(summon.FootholdSN);
            if (summon is Puppet p)
            {
                pw.WriteByte(0); //entertype 1 is broken for puppet in v12, idk why
                pw.WriteByte(0);
                pw.WriteByte(0);
            }
            else
            {
                pw.WriteByte(enterType);
                pw.WriteLong(0); //bMoveability? bassist?
            }
            return pw;
        }

        public static Packet RemoveSummon(Summon summon, byte leaveType)
        {
            Packet pw = new Packet(ServerMessages.SPAWN_LEAVE_FIELD);
            pw.WriteInt(summon.OwnerId);
            pw.WriteInt(summon.SkillId);
            pw.WriteByte(leaveType);
            return pw;
        }

        public static void HandleSummonMove(Character chr, Packet packet)
        {
            var skillId = packet.ReadInt();
            if (chr.Summons.GetSummon(skillId, out var summon))
            {
                var movePath = new MovePath();
                movePath.DecodeFromPacket(packet, MovePath.MovementSource.Summon);
                chr.TryTraceMovement(movePath);

                PacketHelper.ValidateMovePath(summon, movePath);

                SendMoveSummon(chr, summon, movePath);
            }
        }

        private static void SendMoveSummon(Character chr, Summon summon, MovePath movePath)
        {
            Packet pw = new Packet(ServerMessages.SPAWN_MOVE);
            pw.WriteInt(chr.ID);
            pw.WriteInt(summon.SkillId);
            movePath.EncodeToPacket(pw);

            chr.Field.SendPacket(pw, chr);
        }

        public static void HandleSummonDamage(Character chr, Packet packet)
        {
            int summonid = packet.ReadInt();
            if (chr.Summons.GetSummon(summonid, out var summon) && summon is Puppet puppet)
            {
                sbyte unk = packet.ReadSByte();
                int damage = packet.ReadInt();
                int mobid = packet.ReadInt();
                byte unk2 = packet.ReadByte();

                SendDamageSummon(chr, puppet, unk, damage, mobid, unk2);

                //Program.MainForm.LogAppend("Damage: " + damage);
                puppet.TakeDamage(damage);
            }
        }

        private static void SendDamageSummon(Character chr, Puppet summon, sbyte unk, int damage, int mobid, byte unk2)
        {
            // Needs to be fixed.
            Packet pw = new Packet(ServerMessages.SPAWN_HIT);
            pw.WriteInt(chr.ID);
            pw.WriteInt(summon.SkillId);
            pw.WriteSByte(-1);
            pw.WriteInt(damage);
            pw.WriteInt(mobid);

            pw.WriteByte(0);
            pw.WriteLong(0);
            pw.WriteLong(0);
            pw.WriteLong(0);
            chr.Field.SendPacket(pw, chr);
        }


    }
}