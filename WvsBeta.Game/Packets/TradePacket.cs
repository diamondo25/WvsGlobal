using System.Linq;
using log4net;
using WvsBeta.Common.Sessions;

namespace WvsBeta.Game
{
    public static class MiniRoomPacket
    {
        private static ILog miniroomLog = LogManager.GetLogger("MiniroomLog");
        private static ILog miniroomChatLog = LogManager.GetLogger("MiniroomChatLog");

        public static void HandlePacket(Character pCharacter, Packet pPacket)
        {
            //MessagePacket.SendNotice("PACKET: " + pPacket.ToString(), pCharacter);
            byte Type = pPacket.ReadByte();

            switch (Type)
            {
                case 0: // Create miniroom
                    {
                        if (pCharacter.AssertForHack(!pCharacter.CanAttachAdditionalProcess, "Trying to create a miniroom while he cannot attach additional process."))
                        {
                            return;
                        }
                        CreateMiniRoomBase(pCharacter, pPacket);
                        break;
                    }

                case 2: // Invite To miniroom
                    {
                        if (pCharacter.Room == null)
                        {
                            InviteResult(pCharacter, 1);
                            return; // NOT OPENED OR FULL
                        }

                        int playerid = pPacket.ReadInt();
                        Character victim = pCharacter.Field.GetPlayer(playerid);

                        if (victim == null)
                        {
                            miniroomLog.Info($"{pCharacter.Name} fails to invite charid {playerid}: not found?");
                            // Not found!
                            InviteResult(pCharacter, 1);
                        }
                        else if (pCharacter.Room.IsFull())
                        {
                            miniroomLog.Info($"{pCharacter.Name} fails to invite charid {playerid}: room already full?");
                            InviteResult(pCharacter, 2, victim.Name); // DEM REAL DEAL
                        }
                        else if ((pCharacter.IsGM == false && victim.IsGM) ||
                            (pCharacter.IsGM && victim.IsGM == false))
                        {
                            miniroomLog.Info($"{pCharacter.Name} fails to invite charid {playerid}: non-admin tried to invite admin or vice versa");

                            InviteResult(pCharacter, 1);
                        }
                        else
                        {
                            miniroomLog.Info($"{pCharacter.Name} invited {victim.Name} (charid {playerid})");
                            Invite(pCharacter.Room, pCharacter, victim);
                        }

                        break;
                    }

                case 3: // Decline Invite
                    {
                        int roomid = pPacket.ReadInt();

                        miniroomLog.Info($"{pCharacter.Name} declined invite.");
                        if (!MiniRoomBase.MiniRooms.ContainsKey(roomid))
                        {
                            // REPORT
                            //ReportManager.FileNewReport("Tried opening a trade room without a proper ID.", pCharacter.ID, 0);
                            //MessagePacket.SendNotice("Tried opening a trade room without a proper ID. ID was: " + roomid.ToString(), pCharacter);
                            return;
                        }

                        MiniRoomBase mrb = MiniRoomBase.MiniRooms[roomid];
                        //if (mrb.IsFull())
                        //{

                        //}
                        break;
                    }

                case 4: // Enter Room
                    {
                        EnterMiniRoom(pCharacter, pPacket);
                        break;
                    }

                case 0x06: // Chat
                    {
                        if (pCharacter.Room == null) return;

                        var text = pPacket.ReadString();

                        var chatLogLine = pCharacter.Name + ": " + text;
                        if (MessagePacket.ShowMuteMessage(pCharacter))
                        {
                            miniroomChatLog.Info("[MUTED] " + chatLogLine);
                        }
                        else
                        {
                            miniroomChatLog.Info(chatLogLine);
                            Chat(pCharacter.Room, pCharacter, text, -1);
                        }

                        break;
                    }

                case 0x12: //Add item to Player Shop
                    {
                        if (pCharacter.Room == null) return;

                        byte inventory = pPacket.ReadByte();
                        short inventoryslot = pPacket.ReadShort();
                        short bundleamount = pPacket.ReadShort();
                        short AmountPerBundle = pPacket.ReadShort();
                        int price = pPacket.ReadInt();
                        PlayerShop.HandleShopUpdateItem(pCharacter, inventory, inventoryslot, bundleamount, AmountPerBundle, price);
                        break;
                    }

                case 0x13: //Buy item from shop
                    {
                        if (pCharacter.Room == null) return;

                        byte slot = pPacket.ReadByte();
                        short bundleamount = pPacket.ReadShort();
                        PlayerShop ps = MiniRoomBase.PlayerShops[pCharacter.Room.ID];

                        if (ps != null)
                        {
                            ps.BuyItem(pCharacter, slot, bundleamount);
                        }

                        break;
                    }

                case 0xA: //Leave
                    {
                        MiniRoomBase mr = pCharacter.Room;
                        if (mr == null) return;

                        miniroomLog.Info($"{pCharacter.Name} declined invite.");

                        if (mr.Type == MiniRoomBase.RoomType.Trade)
                        {
                            for (int i = 0; i < 2; i++)
                            {
                                Character chr = mr.Users[i];
                                Character leader = mr.Users[0];

                                if (chr == null)
                                {
                                    continue;
                                }

                                mr.RemovePlayer(chr, 1);
                                //mr.Users[i] = null; //send this after all characters are removed
                            }
                        }

                        else if (mr.Type == MiniRoomBase.RoomType.PersonalShop)
                        {
                            mr.RemovePlayerFromShop(pCharacter);
                        }

                        else if (mr.Type == MiniRoomBase.RoomType.Omok)
                        {
                            //MessagePacket.SendNotice("leave omok", pCharacter);
                            Omok omok = MiniRoomBase.Omoks[pCharacter.Room.ID];

                            if (pCharacter == omok.Users[0])
                            {
                                omok.CloseOmok(pCharacter);
                            }
                            else
                            {
                                ShowLeaveRoom(pCharacter.Room, pCharacter, 2);
                                omok.RemovePlayer(pCharacter, 1);
                            }
                        }

                        break;
                    }

                case 0xB: //Add announce box
                    {
                        if (pCharacter.Room == null) return;
                        MiniGamePacket.AddAnnounceBox(pCharacter, (byte)pCharacter.Room.Type, pCharacter.Room.ID, pCharacter.Room.Title, pCharacter.Room.Private, 0, false);
                        byte RoomType = (byte)pCharacter.Room.Type;

                        switch (RoomType)
                        {
                            case 1:
                                {
                                    pCharacter.Field.Omoks.Add(pCharacter.Room.ID, MiniRoomBase.Omoks[pCharacter.Room.ID]);
                                    break;
                                }
                            case 4:
                                {
                                    pCharacter.Field.PlayerShops.Add(pCharacter.Room.ID, MiniRoomBase.PlayerShops[pCharacter.Room.ID]);
                                    break;
                                }
                        }
                        break;
                    }

                case 0x17: //Move Item from player shop to inventory
                    {
                        return;
                        if (pCharacter.AssertForHack(!(pCharacter.Room is PlayerShop), "PlayerShop hack: taking back item while not in playershop")) return;
                        byte slot = pPacket.ReadByte(); //reads as byte, sends as short... wtf lol
                        PlayerShop ps = pCharacter.Room as PlayerShop;
                        if (pCharacter.AssertForHack(ps.Owner != pCharacter, "PlayerShop hack: taking back item while not owner")) return;

                        ps.HandleMoveItemBack(pCharacter, slot);
                        ps.Items.Remove(slot);
                        break;
                    }

                case 0x19: //Request tie result
                    {
                        bool result = pPacket.ReadBool();
                        break;
                    }

                case 0x20: //Ready
                    {
                        MiniGamePacket.Ready(pCharacter, pCharacter.Room);
                        break;
                    }

                case 0x21:
                    {
                        MiniGamePacket.UnReady(pCharacter, pCharacter.Room);
                        break;
                    }

                case 0x22: //Expell user
                    {
                        //Todo : expell
                        break;
                    }

                case 0x23:
                    {
                        Omok omok = MiniRoomBase.Omoks[pCharacter.Room.ID];
                        if (omok != null)
                        {
                            MiniGamePacket.Start(pCharacter, pCharacter.Room);
                            omok.GameStarted = true;
                        }
                        break;
                    }

                case 0x25:
                    {
                        Omok omok = MiniRoomBase.Omoks[pCharacter.Room.ID];
                        omok.UpdateGame(pCharacter);
                        omok.GameStarted = false;
                        break;
                    }

                case 0x26: //Place omok piece
                    {
                        Omok omok = MiniRoomBase.Omoks[pCharacter.Room.ID];

                        if (omok != null)
                        {
                            int X = pPacket.ReadInt();
                            int Y = pPacket.ReadInt();
                            byte Piece = pPacket.ReadByte();

                            if (omok.Stones[X, Y] != Piece && omok.Stones[X, Y] != omok.GetOtherPiece(Piece))
                            {
                                MiniGamePacket.MoveOmokPiece(pCharacter, pCharacter.Room, X, Y, Piece);
                                omok.AddStone(X, Y, Piece, pCharacter);
                            }
                            else
                            {
                                MiniGamePacket.OmokMessage(pCharacter, pCharacter.Room, 0);
                            }
                            //MessagePacket.SendNotice("X : " + X + " Y : " + Y, pCharacter);
                            if (omok.CheckStone(Piece))
                            {
                                //MessagePacket.SendNotice("Win!", pCharacter);
                                omok.UpdateGame(pCharacter);
                                Piece = 0xFF;
                                omok.GameStarted = false;
                            }
                        }

                        break;
                    }

                case 0x1C:
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            if (pCharacter.Room.Users[i] != pCharacter)
                            {
                                MiniGamePacket.RequestHandicap(pCharacter.Room.Users[i], pCharacter.Room);
                            }
                        }

                        break;
                    }

                case 0x1D: //Request handicap result
                    {
                        bool result = pPacket.ReadBool();
                        Omok omok = MiniRoomBase.Omoks[pCharacter.Room.ID];

                        if (omok != null)
                        {
                            if (result == true)
                            {
                                for (int i = 0; i < 2; i++)
                                {
                                    if (pCharacter.Room.Users[i] != pCharacter)
                                    {
                                        if (omok.PlacedStone[i] == false)
                                        {
                                            MiniGamePacket.RequestHandicapResult(pCharacter, pCharacter.Room, result, 2);
                                            omok.TotalStones -= 2;
                                            //MessagePacket.SendNotice("removed", pCharacter);
                                        }
                                        else
                                        {
                                            MiniGamePacket.RequestHandicapResult(pCharacter, pCharacter.Room, result, 1);
                                            omok.TotalStones--;
                                            //omok.Stones[omok.LastPlacedStone[(byte)(pCharacter.RoomSlotId + 1)].mX, omok.LastPlacedStone[(byte)(pCharacter.RoomSlotId + 1)].mY] = 0xFF;
                                            //MessagePacket.SendNotice("Removed stone", pCharacter);
                                        }
                                    }

                                }
                            }
                        }
                        break;
                    }

                default:
                    {
                        if (pCharacter.Room != null)
                        {
                            pCharacter.Room.OnPacket(pCharacter, Type, pPacket);
                        }
                        //MessagePacket.SendNotice("This feature is currently disabled due to maintenance.", pCharacter);
                        break;
                    }
            }
        }

        private static void CreateMiniRoomBase(Character chr, Packet packet)
        {
            if (chr.Room != null)
            {
                return;
            }

            byte nType = packet.ReadByte();

            switch (nType)
            {
                case 0: // What is this case?
                    {
                        break;
                    }

                case 1: // Omok
                    {
                        miniroomLog.Info($"{chr.Name} creates an omok miniroom");
                        MiniRoomBase omok = MiniRoomBase.CreateRoom(chr, 1, packet, false, 0);
                        chr.Room = omok;

                        MiniGamePacket.ShowWindow(chr, omok, MiniRoomBase.Omoks[chr.Room.ID].OmokType);
                        MiniGamePacket.AddAnnounceBox(chr, (byte)MiniRoomBase.RoomType.Omok, omok.ID, omok.Title, omok.Private, omok.PieceType, false);
                        break;
                    }

                case 2: // Match Cards TODO!
                    {
                        return;
                        miniroomLog.Info($"{chr.Name} creates a match cards");
                        string title = packet.ReadString();
                        bool usePassword = packet.ReadBool();
                        string password = "";
                        if (usePassword)
                        {
                            password = packet.ReadString();
                        }
                        packet.Skip(7);
                        byte cardType = packet.ReadByte();
                        break;
                    }

                case 3: // Trade
                    {
                        miniroomLog.Info($"{chr.Name} creates a trade miniroom");
                        MiniRoomBase mrb = MiniRoomBase.CreateRoom(chr, nType, packet, false, 0);
                        chr.Room = mrb;
                        MiniRoomPacket.ShowWindow(mrb, chr);
                        break;
                    }

                case 4: // Player Shops
                    {
                        return;
                        miniroomLog.Info($"{chr.Name} creates a player shop miniroom");
                        MiniRoomBase mrb = MiniRoomBase.CreateRoom(chr, nType, packet, false, 0);
                        chr.Room = mrb;
                        PlayerShopPackets.OpenPlayerShop(chr, mrb);
                        break;
                    }
            }
        }

        private static void EnterMiniRoom(Character chr, Packet packet)
        {
            if (chr.Room != null)
            {
                miniroomLog.Info($"{chr.Name} cannot enter miniroom: already in one.");
                return; // Already in a Mini Room
            }

            
            //MessagePacket.SendNotice("PACKET: " + packet.ToString(), chr);
            int roomId = packet.ReadInt();
            if (!MiniRoomBase.MiniRooms.TryGetValue(roomId, out var mrb))
            {
                ReportManager.FileNewReport("Tried entering a trade room without a proper ID.", chr.ID, 0);
                return; // Invalid Room ID
            }

            if (mrb.EnteredUsers == 0) return;

            if (mrb.IsFull())
            {
                miniroomLog.Info($"{chr.Name} cannot enter miniroom: already full.");
                return; // Error msg if full?
            }

            if (mrb.Users.ToList().Exists(u => u != null && u.MapID != chr.MapID))
            {
                InviteResult(chr, 1); // must be on same map. Show "not found" msg
                return;
            }

            chr.Room = mrb;
            byte nType = (byte)chr.Room.Type;
            switch (nType)
            {
                case 1: // Omok
                    {
                        bool usePassword = packet.ReadBool();
                        Omok omok = MiniRoomBase.Omoks[chr.Room.ID];

                        if (usePassword)
                        {
                            string password = packet.ReadString();
                            if (password != omok.Password)
                            {
                                miniroomLog.Info($"{chr.Name} cannot enter omok: invalid password");
                                MiniGamePacket.ErrorMessage(chr, MiniGamePacket.MiniGameError.IncorrectPassword);
                                chr.Room = null;
                                break;
                            }
                        }
                        if (chr.Inventory.Mesos >= 100)
                        {
                            omok.AddPlayer(chr);
                            MiniGamePacket.AddVisitor(chr, mrb);
                            MiniGamePacket.ShowWindow(chr, mrb, omok.OmokType);
                            chr.AddMesos(-100);
                            miniroomLog.Info($"{chr.Name} entered omok");
                        }
                        else
                        {
                            miniroomLog.Info($"{chr.Name} cannot enter omok: not enough mesos");
                            MiniGamePacket.ErrorMessage(chr, MiniGamePacket.MiniGameError.NotEnoughMesos);
                        }
                        break;
                    }
                case 3: // Trade
                    {
                        miniroomLog.Info($"{chr.Name} entered trade");
                        mrb.AddPlayer(chr);
                        MiniRoomPacket.ShowJoin(mrb, chr);
                        MiniRoomPacket.ShowWindow(mrb, chr);
                        break;
                    }
                case 4: // Player Shop
                    {
                        miniroomLog.Info($"{chr.Name} entered playershop");
                        PlayerShop shop = MiniRoomBase.PlayerShops[roomId];
                        for (int i = 0; i < shop.EnteredUsers; i++)
                        {
                            Character shopUser = mrb.Users[i];
                            if (shopUser != null && shopUser != chr)
                            {
                                shop.AddPlayer(chr);
                                PlayerShopPackets.AddPlayer(chr, shopUser);
                                PlayerShopPackets.OpenPlayerShop(chr, mrb);
                                PlayerShopPackets.PersonalShopRefresh(chr, shop); //Show items 
                            }
                        }
                        break;
                    }
            }
        }

        public static void ShowWindow(MiniRoomBase pRoom, Character pTo)
        {
            Packet pw = new Packet(ServerMessages.MINI_ROOM_BASE);
            pw.WriteByte(5);
            pw.WriteByte((byte)pRoom.Type);
            pw.WriteByte(pRoom.MaxUsers);
            pw.WriteByte(pTo.RoomSlotId);

            for (int i = 0; i < pRoom.Users.Length; i++)
            {
                Character character = pRoom.Users[i];

                if (character == null)
                {
                    continue;
                }

                pw.WriteByte(character.RoomSlotId);
                PacketHelper.AddAvatar(pw, character);
                pw.WriteString(character.Name);
            }

            pw.WriteByte(0xFF);
            pRoom.EncodeEnter(pTo, pw);
            pTo.SendPacket(pw);
        }

        public static void ShowJoin(MiniRoomBase pRoom, Character pWho)
        {
            Packet pw = new Packet(ServerMessages.MINI_ROOM_BASE);
            pw.WriteByte(4);
            pw.WriteByte(pWho.RoomSlotId);
            PacketHelper.AddAvatar(pw, pWho);
            pw.WriteString(pWho.Name);
            pRoom.EncodeEnterResult(pWho, pw);
            pRoom.BroadcastPacket(pw, pWho);
        }

        public static void ShowLeave(MiniRoomBase pRoom, Character pWho, byte pReason)
        {
            Packet pw = new Packet(ServerMessages.MINI_ROOM_BASE);
            pw.WriteByte(0xA);
            pw.WriteByte(pWho.RoomSlotId);
            pw.WriteByte(pReason);
            pWho.SendPacket(pw);
        }

        public static void ShowLeaveRoom(MiniRoomBase pRoom, Character pWho, byte pReason)
        {
            Packet pw = new Packet(ServerMessages.MINI_ROOM_BASE);
            pw.WriteByte(0xA);
            pw.WriteByte(pWho.RoomSlotId);
            pw.WriteByte(pReason);
            pRoom.BroadcastPacket(pw);
        }

        public static void Invite(MiniRoomBase pRoom, Character pWho, Character pVictim)
        {
            Packet pw = new Packet(ServerMessages.MINI_ROOM_BASE);
            pw.WriteByte(2);
            pw.WriteByte((byte)pRoom.Type);
            pw.WriteString(pWho.Name);
            pw.WriteInt(pRoom.ID);
            pVictim.SendPacket(pw);
        }

        public static void InviteResult(Character pWho, byte pFailID, string pName = "")
        {
            Packet pw = new Packet(ServerMessages.MINI_ROOM_BASE);
            pw.WriteByte(3);
            pw.WriteByte(pFailID);

            if (pFailID == 2 || pFailID == 0)
            {
                pw.WriteString(pName);
            }

            pWho.SendPacket(pw);
        }

        public static void Chat(MiniRoomBase pRoom, Character pCharacter, string pText, sbyte pMessageCode)
        {
            Packet pw = new Packet(ServerMessages.MINI_ROOM_BASE);
            pw.WriteByte(6);

            if (pMessageCode < 0)
            {
                pw.WriteByte(8);
                pw.WriteByte(pCharacter.RoomSlotId);
                pw.WriteString($"{pCharacter.Name} : {pText}");

            }
            else
            {
                pw.WriteByte(7);
                pw.WriteSByte(pMessageCode);
                pw.WriteString(pCharacter.Name);
            }

            pRoom.BroadcastPacket(pw);
        }
    }


    public static class TradePacket
    {
        // This packet feels wonky and insecure - wackyracer
        public static void AddItem(Character pTo, byte TradeSlot, BaseItem pItem, byte User)
        {
            int itemType = (pItem.ItemID / 1000000);
            Packet pw = new Packet(ServerMessages.MINI_ROOM_BASE);
            pw.WriteByte(13);
            pw.WriteByte(User); // 0 or 1 based on left/right side of trade window
            pw.WriteByte(TradeSlot); // item slot in the trade window
            pw.WriteByte((byte)itemType); // Item Type (EQ, USE, SETUP, ETC, PET)
            PacketHelper.AddItemData(pw, pItem, 0, false);
            pTo.SendPacket(pw);
        }

        // This is unused. Why? Idk. This has something to do with Trading Stars being bugged. It is probably the fix. - wackyracer
        public static void AddItemWithAmount(Character pTo, byte TradeSlot, BaseItem pItem, short amount, byte User)
        {
            int itemType = (pItem.ItemID / 1000000);
            // Used for items from the same stack
            Packet pw = new Packet(ServerMessages.MINI_ROOM_BASE);
            pw.WriteByte(13);
            pw.WriteByte(User); // 0 or 1 based on left/right side of trade window
            pw.WriteByte(TradeSlot); // item slot in the trade window
            pw.WriteByte((byte)itemType); // Item Type (EQ, USE, SETUP, ETC, PET)
            PacketHelper.AddItemDataWithAmount(pw, pItem, 0, false, amount);
            pTo.SendPacket(pw);
        }

        public static void PutCash(Character pTo, int pAmount, byte test)
        {
            Packet pw = new Packet(ServerMessages.MINI_ROOM_BASE);
            pw.WriteByte(14);
            pw.WriteByte(test);
            pw.WriteInt(pAmount);
            pTo.SendPacket(pw);
        }

        public static void SelectTrade(Character pTo)
        {
            Packet pw = new Packet(ServerMessages.MINI_ROOM_BASE);
            pw.WriteByte(0xF);
            pTo.SendPacket(pw);
        }

        public static void TradeUnsuccessful(Character pTo)
        {
            Packet pw = new Packet(ServerMessages.MINI_ROOM_BASE);
            pw.WriteByte(10);
            pw.WriteByte(pTo.RoomSlotId);
            pw.WriteByte(6);
            pTo.SendPacket(pw);
        }

        public static void TradeSuccessful(Character pCompleter)
        {
            Packet pw = new Packet(ServerMessages.MINI_ROOM_BASE);
            pw.WriteByte(10);
            pw.WriteByte(pCompleter.RoomSlotId);
            pw.WriteByte(5);
            pCompleter.SendPacket(pw);
        }
    }
}