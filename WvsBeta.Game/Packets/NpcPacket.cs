using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;

namespace WvsBeta.Game
{
    public static class NpcPacket
    {

        public static void HandleNPCChat(Character chr, Packet packet)
        {
            if (chr.NpcSession == null)
                return;

            NpcChatSession session = chr.NpcSession;
            byte state = packet.ReadByte();
            if (state != session.mLastSentType)
            {
                InventoryPacket.NoChange(chr);
                return;
            }

            if (!session.WaitingForResponse)
            {
                InventoryPacket.NoChange(chr);
                return;
            }
            session.WaitingForResponse = false;

            Trace.WriteLine(packet.ToString());

            byte option = packet.ReadByte();
            try
            {
                switch (state)
                {
                    case 0:
                        switch (option)
                        {
                            case 0: // Back button...
                                session.SendPreviousMessage();
                                break;
                            case 1: // Next button...
                                session.SendNextMessage();
                                break;
                            default:
                                session.Stop();
                                break;
                        }
                        break;

                    case 1:
                        switch (option)
                        {
                            case 0: // No.
                                session.HandleThing(session.mRealState, 0, "", 0);
                                break;
                            case 1: // Yes.
                                session.HandleThing(session.mRealState, 1, "", 0);
                                break;
                            default:
                                session.Stop();
                                break;
                        }
                        break;

                    case 2:
                        switch (option)
                        {
                            case 0: // No text :(
                                session.Stop();
                                break;
                            case 1: // Oh yea, text
                                session.HandleThing(session.mRealState, 1, packet.ReadString(), 0);
                                break;
                            default:
                                session.Stop();
                                break;
                        }
                        break;

                    case 3:
                        switch (option)
                        {
                            case 0: // No int :(
                                session.Stop();
                                break;
                            case 1: // Oh yea, int
                                session.HandleThing(session.mRealState, 1, "", packet.ReadShort());
                                break;
                            default:
                                session.Stop();
                                break;
                        }
                        break;

                    case 4:
                    case 5:
                        switch (option)
                        {
                            case 0: // Stopping.
                                session.Stop();
                                break;
                            case 1: // Got answer
                                var val = packet.ReadByte();
                                if (val == 255) val = 0; // Menus do not correctly work when holding enter key
                                session.HandleThing(session.mRealState, val, "", 0);
                                break;
                            default:
                                session.Stop();
                                break;
                        }
                        break;

                    default:
                        session.Stop();
                        Program.MainForm.LogAppend("Unknown NPC chat action: " + packet);
                        break;

                }
            }
            catch (Exception ex)
            {
                Program.MainForm.LogAppend($"Exception while handling NPC {session.mID} {session.mRealState}. Packet: " + packet + ". Exception: " + ex);
                InventoryPacket.NoChange(chr);
                session?.Stop();
            }
        }
        
        public enum ShopReq
        {
            Buy = 0,
            Sell,
            Recharge,
            Close,
        }

        public enum ShopRes
        {
            BuySuccess = 0,
            BuyNoStock,
            BuyNoMoney,
            BuyUnknown,
            SellSuccess,
            SellNoStock,
            SellIncorrectRequest,
            SellUnkonwn,
            RechargeSuccess,
            RechargeNoStock,
            RechargeNoMoney,
            RechargeIncorrectRequest,
            RechargeUnknown,
        }

        public static void HandleNPCShop(Character chr, Packet packet)
        {
            if (chr.ShopNPCID == 0) return;

            var shopInfo = DataProvider.NPCs[chr.ShopNPCID].Shop;
            var transferId = "" + chr.ID + "-" + chr.ShopNPCID + "-" + RNG.Range.generate(0, long.MaxValue).ToString();

            byte type = packet.ReadByte();
            switch ((ShopReq)type)
            {
                case ShopReq.Buy:
                    {
                        short slot = packet.ReadShort();
                        int itemid = packet.ReadInt();
                        short amount = packet.ReadShort();

                        if (amount < 1 ||
                            (Constants.isEquip(itemid) && amount != 1))
                        {
                            Program.MainForm.LogAppend("Disconnecting player: trying to buy a negative amount of items OR multiple equips. " + packet);
                            chr.Player.Socket.Disconnect();
                            return;
                        }

                        if (slot < 0 || slot >= shopInfo.Count)
                        {
                            SendShopResult(chr, ShopRes.BuyUnknown);
                            return;
                        }

                        ShopItemData sid = shopInfo[slot];
                        int costs = amount * sid.Price;
                        if (false && sid.Stock == 0)
                        {
                            SendShopResult(chr, ShopRes.BuyNoStock);
                            return;
                        }
                        if (sid.ID != itemid)
                        {
                            SendShopResult(chr, ShopRes.BuyUnknown);
                            return;
                        }
                        if (costs > chr.Inventory.Mesos)
                        {
                            SendShopResult(chr, ShopRes.BuyNoMoney);
                            return;
                        }

                        if (Constants.isRechargeable(itemid))
                        {
                            costs = amount * sid.Price;
                            if (amount > DataProvider.Items[itemid].MaxSlot) // You can't but multiple sets at once
                            {
                                SendShopResult(chr, ShopRes.BuyUnknown);
                                return;
                            }
                        }


                        if (!chr.Inventory.HasSlotsFreeForItem(itemid, amount, true))
                        {
                            SendShopResult(chr, ShopRes.BuyUnknown);
                            return;
                        }

                        Common.Tracking.MesosTransfer.PlayerBuysFromShop(chr.ID, chr.ShopNPCID, costs,
                            transferId);
                        Common.Tracking.ItemTransfer.PlayerBuysFromShop(chr.ID, chr.ShopNPCID, itemid, amount,
                            transferId, null);

                        chr.Inventory.AddNewItem(itemid, amount);
                        SendShopResult(chr, ShopRes.BuySuccess);
                        sid.Stock -= amount;
                        chr.AddMesos(-costs);

                        break;
                    }
                case ShopReq.Sell:
                    {
                        short itemslot = packet.ReadShort();
                        int itemid = packet.ReadInt();
                        short amount = packet.ReadShort();
                        byte inv = Constants.getInventory(itemid);

                        BaseItem item = chr.Inventory.GetItem(inv, itemslot);

                        if (item == null ||
                            item.ItemID != itemid ||
                            amount < 1 ||
                            // Do not trigger this when selling stars and such.
                            (!Constants.isRechargeable(itemid) && amount > item.Amount) ||
                            (Constants.isEquip(itemid)
                            ? DataProvider.Equips.ContainsKey(itemid) == false
                            : DataProvider.Items.ContainsKey(itemid) == false) ||
                            item.CashId != 0)
                        {
                            Program.MainForm.LogAppend("Disconnecting player: invalid trade packet: " + packet);
                            chr.Player.Socket.Disconnect();
                            return;
                        }


                        int sellPrice = 0;
                        if (Constants.isEquip(itemid))
                        {
                            var ed = DataProvider.Equips[itemid];
                            sellPrice = ed.Price;
                        }
                        else
                        {
                            var id = DataProvider.Items[itemid];
                            sellPrice = id.Price * amount;
                        }

                        if (sellPrice < 0)
                        {
                            SendShopResult(chr, ShopRes.SellIncorrectRequest);
                            return;
                        }

                        // Change amount here (rechargeables are sold as 1)
                        if (Constants.isRechargeable(item.ItemID))
                        {
                            amount = item.Amount;
                        }

                        Common.Tracking.MesosTransfer.PlayerSellsToShop(chr.ID, chr.ShopNPCID, sellPrice, transferId);
                        Common.Tracking.ItemTransfer.PlayerSellsToShop(chr.ID, chr.ShopNPCID, item.ItemID, amount, transferId, item);

                        if (amount == item.Amount)
                        {
                            chr.Inventory.SetItem(inv, itemslot, null);
                            chr.Inventory.TryRemoveCashItem(item);
                            InventoryPacket.SwitchSlots(chr, itemslot, 0, inv);
                        }
                        else
                        {
                            item.Amount -= amount;
                            InventoryPacket.AddItem(chr, inv, item, false);
                        }
                        chr.AddMesos(sellPrice);

                        SendShopResult(chr, ShopRes.SellSuccess);
                        break;
                    }
                case ShopReq.Recharge:
                    {
                        short itemslot = packet.ReadShort();

                        byte inv = 2;
                        BaseItem item = chr.Inventory.GetItem(inv, itemslot);
                        if (item == null ||
                            !Constants.isRechargeable(item.ItemID))
                        {
                            Program.MainForm.LogAppend("Disconnecting player: invalid trade packet: " + packet);
                            chr.Player.Socket.Disconnect();
                            return;
                        }

                        ShopItemData sid = shopInfo.FirstOrDefault((a) => a.ID == item.ItemID);
                        if (sid == null)
                        {
                            Program.MainForm.LogAppend("Disconnecting player: Item not found in shop; not rechargeable?");
                            chr.Player.Socket.Disconnect();
                            return;
                        }

                        if (sid.UnitRechargeRate <= 0.0)
                        {
                            SendShopResult(chr, ShopRes.RechargeIncorrectRequest);
                            return;
                        }

                        ItemData data = DataProvider.Items[item.ItemID];
                        short maxslot = (short)(data.MaxSlot + chr.Skills.GetRechargeableBonus());
                        short toFill = (short)(maxslot - item.Amount);

                        int sellPrice = (int)Math.Ceiling(-1.0 * sid.UnitRechargeRate * toFill);
                        sellPrice = Math.Max(sellPrice, 1);
                        if (chr.Inventory.Mesos > -sellPrice)
                        {
                            Common.Tracking.MesosTransfer.PlayerBuysFromShop(chr.ID, chr.ShopNPCID, -sellPrice,
                                transferId);
                            Common.Tracking.ItemTransfer.PlayerBuysFromShop(chr.ID, chr.ShopNPCID, item.ItemID,
                                (short)(maxslot - item.Amount), transferId, item);

                            item.Amount = maxslot;

                            chr.AddMesos(sellPrice);
                            InventoryPacket.AddItem(chr, inv, item, false);
                            SendShopResult(chr, ShopRes.RechargeSuccess);
                        }
                        else
                        {
                            SendShopResult(chr, ShopRes.RechargeNoMoney); // no muney? hier! suk a kok!
                        }
                        break;
                    }
                case ShopReq.Close: chr.ShopNPCID = 0; chr.NpcSession = null; break;
                default:
                    {
                        Program.MainForm.LogAppend("Unknown NPC shop action: " + packet);
                        break;
                    }

            }

        }

        public static void SendShowNPCShop(Character chr, int NPCID)
        {
            Packet pw = new Packet(ServerMessages.SHOP);
            pw.WriteInt(NPCID);

            List<ShopItemData> ShopItems = DataProvider.NPCs[NPCID].Shop;

            ushort maxSlots = 1;

            pw.WriteShort((short)ShopItems.Count);
            foreach (ShopItemData item in ShopItems)
            {
                pw.WriteInt(item.ID);
                pw.WriteInt(item.Price);

                if (DataProvider.Items.TryGetValue(item.ID, out ItemData id))
                {
                    maxSlots = id.MaxSlot;
                    if (maxSlots == 0)
                    {
                        // 1, 100 or specified
                        maxSlots = 100;
                    }
                }
                if (Constants.isRechargeable(item.ID))
                {
                    pw.WriteLong(BitConverter.DoubleToInt64Bits(item.UnitRechargeRate));
                    maxSlots += chr.Skills.GetRechargeableBonus();
                }

                pw.WriteUShort(maxSlots);
                maxSlots -= chr.Skills.GetRechargeableBonus();
            }
            pw.WriteLong(0);
            pw.WriteLong(0);
            pw.WriteLong(0);

            chr.SendPacket(pw);
        }

        public static void SendShopResult(Character chr, ShopRes ans)
        {
            Packet pw = new Packet(ServerMessages.SHOP_TRANSACTION);
            pw.WriteByte((byte)ans);

            chr.SendPacket(pw);
        }

        public static void SendNPCChatTextSimple(Character chr, int NpcID, string Text, bool back, bool next)
        {
            chr.NpcSession.mLastSentType = 0;
            Packet pw = new Packet(ServerMessages.SCRIPT_MESSAGE);
            pw.WriteByte(0x04);
            pw.WriteInt(NpcID);
            pw.WriteByte(0);
            pw.WriteString(Text);
            pw.WriteBool(back);
            pw.WriteBool(next);

            chr.SendPacket(pw);
        }

        public static void SendNPCChatTextMenu(Character chr, int NpcID, string Text)
        {
            chr.NpcSession.mLastSentType = 4;
            Packet pw = new Packet(ServerMessages.SCRIPT_MESSAGE);
            pw.WriteByte(0x04);
            pw.WriteInt(NpcID);
            pw.WriteByte(0x04);
            pw.WriteString(Text);

            chr.SendPacket(pw);
        }

        public static void SendNPCChatTextYesNo(Character chr, int NpcID, string Text)
        {
            chr.NpcSession.mLastSentType = 1;
            Packet pw = new Packet(ServerMessages.SCRIPT_MESSAGE);
            pw.WriteByte(0x04);
            pw.WriteInt(NpcID);
            pw.WriteByte(0x01);
            pw.WriteString(Text);

            chr.SendPacket(pw);
        }

        public static void SendNPCChatTextRequestText(Character chr, int NpcID, string Text, string Default, short MinLength, short MaxLength)
        {
            chr.NpcSession.mLastSentType = 2;
            Packet pw = new Packet(ServerMessages.SCRIPT_MESSAGE);
            pw.WriteByte(0x04);
            pw.WriteInt(NpcID);
            pw.WriteByte(0x02);
            pw.WriteString(Text);
            pw.WriteString(Default);
            pw.WriteShort(MinLength);
            pw.WriteShort(MaxLength);

            chr.SendPacket(pw);
        }

        public static void SendNPCChatTextRequestInteger(Character chr, int NpcID, string Text, int Default, int MinValue, int MaxValue)
        {
            chr.NpcSession.mLastSentType = 3;
            Packet pw = new Packet(ServerMessages.SCRIPT_MESSAGE);
            pw.WriteByte(0x04);
            pw.WriteInt(NpcID);
            pw.WriteByte(0x03);
            pw.WriteString(Text);
            pw.WriteInt(Default);
            pw.WriteInt(MinValue);
            pw.WriteInt(MaxValue);

            chr.SendPacket(pw);
        }

        public static void SendNPCChatTextRequestStyle(Character chr, int NpcID, string Text, List<int> values)
        {
            chr.NpcSession.mLastSentType = 5;
            Packet pw = new Packet(ServerMessages.SCRIPT_MESSAGE);
            pw.WriteByte(0x04);
            pw.WriteInt(NpcID);
            pw.WriteByte(0x05);
            pw.WriteString(Text);
            pw.WriteByte((byte)values.Count);
            foreach (int value in values)
            {
                pw.WriteInt(value);
            }

            chr.SendPacket(pw);
        }

        public static void SendNPCChatTextRequestPet(Character chr, int NpcID, string Text)
        {
            chr.NpcSession.mLastSentType = 5;
            Packet pw = new Packet(ServerMessages.SCRIPT_MESSAGE);
            pw.WriteByte(0x04);
            pw.WriteInt(NpcID);
            pw.WriteByte(0x06);
            pw.WriteString(Text);

            var pets = chr.Inventory.GetAlivePets().ToList();

            pw.WriteByte((byte)pets.Count());
            foreach (var petItem in pets)
            {
                pw.WriteLong(petItem.CashId);
                pw.WriteByte((byte)petItem.InventorySlot);
            }

            chr.SendPacket(pw);
        }
    }
}