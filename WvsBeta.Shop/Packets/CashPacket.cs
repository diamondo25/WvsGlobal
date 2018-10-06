using log4net;
using MySql.Data.MySqlClient;
using System;
using System.Linq;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;
using WvsBeta.Game;
using WvsBeta.SharedDataProvider;

namespace WvsBeta.Shop
{
    public static class CashPacket
    {
        private static readonly ILog _log = LogManager.GetLogger("CashPacket");

        public struct BuyItem
        {
            public LockerItem lockerItem { get; set; }
            public bool withMaplePoints { get; set; }
            public int cashAmount { get; set; }
        }

        public struct BuySlotIncrease
        {
            public byte inventory { get; set; }
            public byte newSlots { get; set; }
            public bool withMaplePoints { get; set; }
            public int cashAmount { get; set; }
        }

        public enum CashErrors
        {
            UnknownError = 0x00, // Default statement

            UnknownErrorDC_1 = 80,
            TimeRanOutTryingToProcessRequest_TryAgain = 81,
            UnknownErrorDC_2 = 82,

            NotEnoughCash = 83,
            CantGiftUnder14Year = 84,
            ExceededAllottedLimitOfPriceForGifts = 85,
            CheckExceededNumberOfCashItems = 86,
            CheckCharacterNameOrItemRestrictions = 87,
            CheckCouponNumber = 88,

            DueGenderRestrictionsNoCouponUse = 91,
            CouponOnlyForRegularItemsThusNoGifting = 92,
            CheckFullInventory = 93,
            ItemOnlyAvailableForUsersAtPremiumInternetCafe = 94,
            CoupleItemsCanBeGivenAsAGiftToACharOfDiffGenderAtSameWorld = 95,
            ItemsAreNotAvailableForPurchaseAtThisHour = 96,
            OutOfStock = 97,
            ExceededSpendingLimitOfCash = 98,
            NotEnoughMesos = 99,
            UnavailableDuringBetaTestPhase = 100,
            InvalidDoBEntered = 101,
        }

        public enum CashPacketOpcodes
        {
            // Note: Storage == inventory
            // Client packets (C)
            C_BuyItem = 2,
            C_GiftItem = 3,
            C_UpdateWishlist = 4,
            C_IncreaseSlots = 5,
            C_MoveLtoS = 10,
            C_MoveStoL = 11,

            // Server packets (S)
            S_LoadLocker_Done = 28,
            S_LoadLocker_Failed,
            S_LoadWish_Done = 30,
            S_LoadWish_Failed,
            S_UpdateWish_Done = 32,
            S_UpdateWish_Failed,
            S_Buy_Done = 34,
            S_Buy_Failed,

            S_Gift_Done = 41,
            S_Gift_Failed,

            S_IncSlotCount_Done,
            S_IncSlotCount_Failed,
            S_IncTrunkCount_Done,
            S_IncTrunkCount_Failed,
            //S_IncCharSlotCount_Done,
            //S_IncCharSlotCount_Failed,



            S_MoveLtoS_Done = 47,
            S_MoveLtoS_Failed,
            S_MoveStoL_Done = 49,
            S_MoveStoL_Failed,
            S_Delete_Done = 51, // + long SN
            S_Delete_Failed,
            S_Expired_Done = 53, // + long SN
            S_Expired_Failed,


            S_GiftPackage_Done = 72, // + Itemdata, str, int, short ??
        }


        private static LockerItem CreateLockerItem(int userId, CommodityInfo ci, string buyCharacterName)
        {
            var expiration = ci.Period > 0 ? Tools.GetFileTimeWithAddition(new TimeSpan(ci.Period, 0, 0, 0)) : BaseItem.NoItemExpiration;
            var item = new LockerItem()
            {
                ItemId = ci.ItemID,
                Amount = ci.Count,
                CashId = 0, // Will be created on insert
                Expiration = expiration,
                BuyCharacterName = buyCharacterName, // Empty, only set when gift
                CharacterId = 0, // 0, as its in the locker
                CommodityId = ci.SerialNumber,
                GiftUnread = string.IsNullOrEmpty(buyCharacterName) == false,
                UserId = userId
            };
            return item;
        }

        public static void HandleCashPacket(Character chr, Packet packet)
        {
            var header = packet.ReadByte();
            switch ((CashPacketOpcodes)header)
            {
                case CashPacketOpcodes.C_IncreaseSlots:
                    {
                        var maplepoints = packet.ReadBool();
                        var inventory = packet.ReadByte();

                        if (!(inventory >= 1 && inventory <= 5))
                        {
                            _log.Warn("Increase slots failed: Invalid inventory");
                            SendError(chr, CashPacketOpcodes.S_IncSlotCount_Failed, CashErrors.OutOfStock);
                            return;
                        }

                        var points = chr.GetCashStatus();
                        var price = 4000;

                        if (price > (maplepoints ? points.maplepoints : points.nx))
                        {
                            _log.Warn("Increase slots failed: Not enough NX or maplepoints");
                            SendError(chr, CashPacketOpcodes.S_IncSlotCount_Failed, CashErrors.NotEnoughCash);
                            return;
                        }

                        var slots = chr.Inventory.MaxSlots[inventory - 1];

                        // Client sided limit
                        if (slots > 80)
                        {
                            _log.Warn($"Increase slots failed: already has {slots} slots on inventory {inventory}");
                            SendError(chr, CashPacketOpcodes.S_IncSlotCount_Failed, CashErrors.UnknownErrorDC_1);
                            return;
                        }

                        // no limiting atm
                        slots += 4;
                        chr.Inventory.SetInventorySlots(inventory, slots, false);

                        chr.AddSale($"Bought inventory expansion for inventory type {inventory} character {chr.ID}", price, maplepoints ? Character.TransactionType.MaplePoints : Character.TransactionType.NX);

                        Character.CashLog.Info(new BuySlotIncrease
                        {
                            cashAmount = price,
                            inventory = inventory,
                            newSlots = slots,
                            withMaplePoints = maplepoints
                        });

                        SendIncreasedSlots(chr, inventory, slots);
                        SendCashAmounts(chr);
                        break;
                    }
                case CashPacketOpcodes.C_BuyItem:
                    {
                        var maplepoints = packet.ReadBool();

                        var id = packet.ReadInt();
                        if (!DataProvider.Commodity.TryGetValue(id, out var ci))
                        {
                            _log.Warn($"Buying failed: commodity not found for SN {id}");
                            SendError(chr, CashPacketOpcodes.S_Buy_Failed, CashErrors.OutOfStock);
                            return;
                        }

                        if (ci.OnSale == false ||
                            ci.StockState == StockState.NotAvailable ||
                            ci.StockState == StockState.OutOfStock)
                        {
                            _log.Warn($"Buying failed: commodity {id} not on sale {ci.OnSale} or out of stock {ci.StockState}");
                            SendError(chr, CashPacketOpcodes.S_Buy_Failed, CashErrors.OutOfStock);
                            return;
                        }

                        var points = chr.GetCashStatus();
                        if (ci.Gender != CommodityGenders.Both && (byte)ci.Gender != chr.Gender)
                        {
                            _log.Warn("Buying failed: invalid gender");
                            SendError(chr, CashPacketOpcodes.S_Buy_Failed, CashErrors.UnknownErrorDC_1);
                            return;
                        }

                        if (ci.Price > (maplepoints ? points.maplepoints : points.nx))
                        {
                            _log.Warn("Buying failed: not enough NX or maplepoints");
                            SendError(chr, CashPacketOpcodes.S_Buy_Failed, CashErrors.NotEnoughCash);
                            return;
                        }

                        var lockerItem = CreateLockerItem(chr.UserID, ci, "");
                        var baseItem = CharacterCashLocker.CreateCashItem(lockerItem, ci);
                        chr.Locker.AddItem(lockerItem, baseItem);

                        chr.AddSale($"Bought cash item {lockerItem.ItemId} amount {lockerItem.Amount} (ref: {lockerItem.CashId:X16})", ci.Price, maplepoints ? Character.TransactionType.MaplePoints : Character.TransactionType.NX);

                        Character.CashLog.Info(new BuyItem
                        {
                            cashAmount = ci.Price,
                            lockerItem = lockerItem,
                            withMaplePoints = maplepoints
                        });

                        SendBoughtItem(chr, lockerItem);
                        SendCashAmounts(chr);

                        break;
                    }
                case CashPacketOpcodes.C_GiftItem:
                    {
                        var dob = packet.ReadUInt();
                        var sn = packet.ReadInt();
                        var recipient = packet.ReadString();

                        // check DoB
                        if (chr.DoB != dob)
                        {
                            _log.Warn($"Gifting failed: invalid DoB entered");
                            SendError(chr, CashPacketOpcodes.S_Gift_Failed, CashErrors.InvalidDoBEntered);
                            return;
                        }

                        // Check SN

                        if (!DataProvider.Commodity.TryGetValue(sn, out var ci))
                        {
                            _log.Warn($"Gifting failed: commodity not found for SN {sn}");
                            SendError(chr, CashPacketOpcodes.S_Gift_Failed, CashErrors.OutOfStock);
                            return;
                        }

                        if (ci.OnSale == false ||
                            ci.StockState == StockState.NotAvailable ||
                            ci.StockState == StockState.OutOfStock)
                        {
                            _log.Warn($"Gifting failed: commodity {sn} not on sale {ci.OnSale} or out of stock {ci.StockState}");
                            SendError(chr, CashPacketOpcodes.S_Gift_Failed, CashErrors.OutOfStock);
                            return;
                        }

                        // Check price
                        var points = chr.GetCashStatus();
                        if (ci.Price > points.nx)
                        {
                            _log.Warn("Gifting failed: not enough NX");
                            SendError(chr, CashPacketOpcodes.S_Gift_Failed, CashErrors.NotEnoughCash);
                            return;
                        }
                        
                        // Check recipient
                        int recipientId = 0;
                        int recipientUserId = 0;
                        int recipientGender = 0;
                        using (var mdr = (MySqlDataReader)Server.Instance.CharacterDatabase.RunQuery(
                            "SELECT ID, userid, gender FROM characters WHERE `name` = @name",
                            "@name", recipient
                        ))
                        {
                            if (!mdr.Read())
                            {
                                // Not found
                                _log.Warn($"Gifting failed: character named {recipient} not found");
                                SendError(chr, CashPacketOpcodes.S_Gift_Failed, CashErrors.CheckCharacterNameOrItemRestrictions);
                                return;
                            }

                            recipientId = mdr.GetInt32(0);
                            recipientUserId = mdr.GetInt32(1);
                            recipientGender = mdr.GetInt32(2);
                        }

                        if (ci.Gender != CommodityGenders.Both && recipientGender != (int)ci.Gender)
                        {
                            _log.Warn($"Gifting failed: receipient not {ci.Gender}"); ;
                            SendError(chr, CashPacketOpcodes.S_Gift_Failed, CashErrors.CheckCharacterNameOrItemRestrictions);
                            return;
                        }


                        var lockerItem = CreateLockerItem(recipientUserId, ci, chr.Name);
                        var baseItem = CharacterCashLocker.CreateCashItem(lockerItem, ci);
                        // !!! We are saving the item to the current user, so we can save it alltogether at once!!!!
                        // !!! THIS MEANS THAT IF SOMEONE MANAGED TO CRASH THE CASHSHOP, NOTHING IS LOST !!!!
                        chr.Locker.AddItem(lockerItem, baseItem);

                        chr.AddSale($"Bought cash item {lockerItem.ItemId} amount {lockerItem.Amount} (ref: {lockerItem.CashId:X16}) as a gift for {recipient}", ci.Price, Character.TransactionType.NX);

                        Character.CashLog.Info(new BuyItem
                        {
                            cashAmount = ci.Price,
                            lockerItem = lockerItem,
                            withMaplePoints = false
                        });

                        SendGiftDone(chr, lockerItem, recipient);
                        SendCashAmounts(chr);
                        break;
                    }

                case CashPacketOpcodes.C_UpdateWishlist:
                    {
                        for (byte i = 0; i < 10; i++)
                        {
                            var val = packet.ReadInt();

                            if (val == 0 || DataProvider.Commodity.ContainsKey(val))
                            {
                                chr.Wishlist[i] = val;
                            }
                            else
                            {
                                chr.Wishlist[i] = 0;
                                _log.Warn($"While updating wishlist, commodity not found for SN {val}");
                            }
                        }

                        SendWishlist(chr, true);
                        break;
                    }
                case CashPacketOpcodes.C_MoveStoL:
                    {
                        var cashid = packet.ReadLong();
                        var inv = packet.ReadByte();

                        var lockerItem = chr.Inventory.GetLockerItemByCashID(cashid);
                        if (lockerItem == null)
                        {
                            _log.Warn($"Moving Storage to Locker failed: locker item not found with cashid {cashid}");
                            SendError(chr, CashPacketOpcodes.S_MoveStoL_Failed, CashErrors.UnknownError);
                            return;
                        }

                        if (Constants.getInventory(lockerItem.ItemId) != inv)
                        {
                            _log.Warn($"Moving Storage to Locker failed: inventory didn't match.");
                            SendError(chr, CashPacketOpcodes.S_MoveStoL_Failed, CashErrors.UnknownError);
                            return;
                        }

                        var item = chr.Inventory.GetItemByCashID(cashid, inv);

                        lockerItem.CharacterId = 0; // Reset

                        chr.Inventory.RemoveLockerItem(lockerItem, item, false);
                        chr.Locker.AddItem(lockerItem, item);

                        SendPlacedItemInStorage(chr, lockerItem);

                        break;
                    }
                case CashPacketOpcodes.C_MoveLtoS:
                    {
                        var cashid = packet.ReadLong();
                        var inv = packet.ReadByte();
                        var slot = packet.ReadShort();

                        var lockerItem = chr.Locker.GetLockerItemFromCashID(cashid);
                        if (lockerItem == null)
                        {
                            _log.Warn($"Moving Locker to Storage failed: locker item not found with cashid {cashid}");
                            SendError(chr, CashPacketOpcodes.S_MoveLtoS_Failed, CashErrors.UnknownError);
                            return;
                        }

                        if (Constants.getInventory(lockerItem.ItemId) != inv)
                        {
                            _log.Warn($"Moving Locker to Storage failed: inventory didn't match.");
                            SendError(chr, CashPacketOpcodes.S_MoveLtoS_Failed, CashErrors.UnknownError);
                            return;
                        }

                        if (lockerItem.UserId != chr.UserID)
                        {
                            _log.Warn($"Moving Locker to Storage failed: tried to move cash item that was not from himself (packethack?)");
                            SendError(chr, CashPacketOpcodes.S_MoveLtoS_Failed, CashErrors.UnknownError);
                            return;
                        }

                        var item = chr.Locker.GetItemFromCashID(cashid, lockerItem.ItemId);
                        if (item == null)
                        {
                            _log.Warn($"Moving Locker to Storage failed: item not found with cashid {cashid} itemid {lockerItem.ItemId}");
                            SendError(chr, CashPacketOpcodes.S_MoveLtoS_Failed, CashErrors.UnknownError);
                            return;
                        }

                        if (slot < 1 || slot > chr.Inventory.MaxSlots[inv - 1])
                        {
                            _log.Warn($"Moving Locker to Storage failed: not enough slots left.");
                            SendError(chr, CashPacketOpcodes.S_MoveLtoS_Failed, CashErrors.CheckFullInventory);
                            return;
                        }

                        if (chr.Inventory.GetItem(inv, slot) != null)
                        {
                            _log.Warn($"Moving Locker to Storage failed: slot is not empty");
                            SendError(chr, CashPacketOpcodes.S_MoveLtoS_Failed, CashErrors.UnknownError);
                            return;
                        }

                        lockerItem.CharacterId = chr.ID;

                        chr.Inventory.AddLockerItem(lockerItem);
                        chr.Inventory.AddItem(inv, slot, item, false);
                        chr.Locker.RemoveItem(lockerItem, item);

                        SendPlacedItemInInventory(chr, item);
                        break;
                    }
                default:
                    {
                        //string what = "[" + DateTime.Now.ToString() + ":" + DateTime.Now.Millisecond.ToString("D3") + "] Unknown packet found: " + packet.ToString();
                        //FileWriter.WriteLine(@"connection_log\" + chr.mID.ToString() + ".txt", what, true);
                        ////Console.WriteLine("Unknown packet received! " + packet.ToString());
                        Program.MainForm.LogAppend("Unknown data: " + packet);
                        break;
                    }
            }
        }

        public static void SendWishlist(Character chr, bool update)
        {
            var pw = GetPacketWriter(update ? CashPacketOpcodes.S_UpdateWish_Done : CashPacketOpcodes.S_LoadWish_Done);
            foreach (var val in chr.Wishlist)
            {
                pw.WriteInt(val);
            }
            chr.SendPacket(pw);
        }

        public static void SendInfo(Character chr)
        {
            SendCashAmounts(chr);
            SendWishlist(chr, false);
            SendLocker(chr);
            //ShowGifts(chr);
        }

        private static Packet GetPacketWriter(CashPacketOpcodes opcode)
        {
            var pw = new Packet(ServerMessages.CASHSHOP_ACTION);
            pw.WriteByte((byte)opcode);
            return pw;
        }

        public static void SendLocker(Character chr)
        {
            var pw = GetPacketWriter(CashPacketOpcodes.S_LoadLocker_Done);

            var userLocker = chr.Locker.Items.Where(x => x.UserId == chr.UserID).ToList();

            pw.WriteByte((byte)userLocker.Count);

            foreach (var item in userLocker)
            {
                item.Encode(pw);
                item.GiftUnread = false;
            }

            pw.WriteShort(3); // Storage slots
            chr.SendPacket(pw);
        }

        public static void SendBoughtItem(Character chr, LockerItem item)
        {
            var pw = GetPacketWriter(CashPacketOpcodes.S_Buy_Done);

            item.Encode(pw);
            chr.SendPacket(pw);
        }

        public static void SendGiftDone(Character chr, LockerItem item, string receipient)
        {
            var pw = GetPacketWriter(CashPacketOpcodes.S_Gift_Done);

            pw.WriteString(receipient);
            pw.WriteInt(item.ItemId);
            pw.WriteShort(item.Amount);
            chr.SendPacket(pw);
        }


        public static void SendIncreasedSlots(Character chr, byte inventory, short slots)
        {
            var pw = GetPacketWriter(CashPacketOpcodes.S_IncSlotCount_Done);
            pw.WriteByte(inventory);
            pw.WriteShort(slots);
            chr.SendPacket(pw);
        }

        public static void SendPlacedItemInInventory(Character chr, BaseItem item)
        {
            var pw = GetPacketWriter(CashPacketOpcodes.S_MoveLtoS_Done);
            pw.WriteShort(item.InventorySlot);
            pw.WriteByte(Constants.getInventory(item.ItemID));
            item.Encode(pw);
            chr.SendPacket(pw);
        }

        public static void SendPlacedItemInStorage(Character chr, LockerItem item)
        {
            var pw = GetPacketWriter(CashPacketOpcodes.S_MoveStoL_Done);
            item.Encode(pw);
            chr.SendPacket(pw);
        }


        public static void SendError(Character chr, CashPacketOpcodes opcode, CashErrors error, int v = 0)
        {
            var pw = new Packet(ServerMessages.CASHSHOP_ACTION);
            pw.WriteByte((byte)opcode);
            pw.WriteByte((byte)error);
            pw.WriteInt(v);

            chr.SendPacket(pw);
        }

        public static void SendCashAmounts(Character chr)
        {
            var points = chr.GetCashStatus();

            var pw = new Packet(ServerMessages.CASHSHOP_UPDATE_AMOUNTS);
            pw.WriteInt(points.nx);
            pw.WriteInt(points.maplepoints);
            chr.SendPacket(pw);
        }

        public static void ShowGifts(Character chr)
        {
            //DecodeBuffer (40 bytes)
            var pw = new Packet(ServerMessages.CASHSHOP_ACTION);
            pw.WriteByte(0x1E);
            /**
            //pw.WriteShort(0);
            Item item = new Item(chr.mStorage.GetCashItem(42));
            PacketHelper.AddGiftList(pw, item);
             * **/
            pw.WriteString("fasfsa", 13);
            pw.WriteString("asfas", 73);
            chr.SendPacket(pw);

        }

        public static void Charge(Character chr)
        {

            //This minimizes your client :O 
            var pw = new Packet(ServerMessages.CASHSHOP_RECHARGE);
            pw.WriteString("C:\\Program Files (x86)\\Internet Explorer\\iexplore.exe");
            pw.WriteString("C:\\Program Files (x86)\\Internet Explorer\\iexplore.exe"); //path? xd
            pw.WriteByte(1);
            pw.WriteShort(1);
            pw.WriteInt(5000000);
            pw.WriteInt(5000000);
            chr.SendPacket(pw);
        }

    }
}