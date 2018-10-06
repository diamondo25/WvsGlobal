using System.Collections.Generic;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;
using WvsBeta.Common.Tracking;

namespace WvsBeta.Game
{
    public class Trade : MiniRoomBase
    {
        public class TradeItem
        {
            public BaseItem OriginalItem { get; set; }
        }

        public bool[] Locked;
        private TradeItem[][] ItemList;

        public Character Owner { get; private set; }

        private int[] Mesos;

        public Trade(Character pOwner) : base(2, RoomType.Trade)
        {
            Owner = pOwner;
            ItemList = new TradeItem[2][];
            ItemList[0] = new TradeItem[10];
            ItemList[1] = new TradeItem[10];
            Locked = new bool[2] { false, false };
            Mesos = new int[2] { 0, 0 };

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    ItemList[i][j] = null;
                }
            }
        }

        public override void Close(byte pReason)
        {
            base.Close(pReason);
            Owner = null;
            ItemList = null;
        }

        private void RevertItems()
        {
            for (int i = 0; i < 2; i++)
            {
                Character chr = Users[i];

                if (chr == null)
                {
                    continue;
                }

                for (int j = 0; j < 10; j++)
                {
                    TradeItem ti = ItemList[i][j];

                    if (ti?.OriginalItem != null) //just to make sure that the player actually has items in trade..
                    {
                        chr.Inventory.AddItem2(ti.OriginalItem);
                        ItemTransfer.PlayerTradeReverted(chr.ID, ti.OriginalItem.ItemID, ti.OriginalItem.Amount, _transaction, ti.OriginalItem);
                        ti.OriginalItem = null;
                    }
                }
            }
        }

        public void CompleteTrade()
        {
            Character pCharacter1 = Users[0];
            Character pCharacter2 = Users[1];
            AddItems(pCharacter1);
            AddItems(pCharacter2);
            pCharacter1.Room = null;
            pCharacter1.RoomSlotId = 0;
            pCharacter1.Save();

            pCharacter2.Room = null;
            pCharacter2.RoomSlotId = 0;
            pCharacter2.Save();
            
            EnteredUsers = 0;
            Close(0);
        }

        private bool ContinueTrade()
        {
            // Both Inventories are checked, and have room
            return CheckInventory(Users[0]) && CheckInventory(Users[1]);
        }

        private bool CheckInventory(Character chr)
        {
            var neededSlots = new Dictionary<byte, int>();

            for (int j = 0; j < 2; j++)
            {
                for (int i = 0; i < 10; i++)
                {
                    if (j == chr.RoomSlotId) continue;
                    TradeItem ti = ItemList[j][i];

                    if (ti == null || ti.OriginalItem == null) continue;

                    byte inv = Constants.getInventory(ti.OriginalItem.ItemID);

                    if (!neededSlots.ContainsKey(inv))
                    {
                        neededSlots.Add(inv, 1);
                    }
                    else
                    {
                        neededSlots[inv] += 1;
                    }
                }
            }
            foreach (var lol in neededSlots)
            {
                if (chr.Inventory.GetOpenSlotsInInventory(lol.Key) < neededSlots[lol.Key])
                {
                    return false;
                }
            }
            return true;
        }


        private void AddItems(Character chr)
        {
            // Note: Exchange logic, so A gets B and B gets A stuff
            for (int i = 0; i < 2; i++)
            {
                if (i == chr.RoomSlotId) continue;
                var charFrom = Users[i];

                for (int j = 0; j < 10; j++)
                {
                    TradeItem ti = ItemList[i][j];

                    if (ti?.OriginalItem != null)
                    {
                        chr.Inventory.AddItem2(ti.OriginalItem);
                        ItemTransfer.PlayerTradeExchange(charFrom.ID, chr.ID, ti.OriginalItem.ItemID, ti.OriginalItem.Amount, _transaction, ti.OriginalItem);
                        ti.OriginalItem = null;
                    }
                }

                var mesos = Mesos[i];
                if (mesos != 0)
                {
                    chr.AddMesos(mesos);
                    MesosTransfer.PlayerTradeExchange(charFrom.ID, chr.ID, mesos, _transaction);
                }
            }
        }


        public override void RemovePlayer(Character pCharacter, byte pReason)
        {
            // Give items back
            RevertItems();

            var mesos = Mesos[pCharacter.RoomSlotId];
            if (mesos != 0)
            {
                pCharacter.AddMesos(mesos);
                MesosTransfer.PlayerTradeReverted(pCharacter.ID, mesos, _transaction);
                Mesos[pCharacter.RoomSlotId] = 0;
            }

            base.RemovePlayer(pCharacter, pReason);
        }

        public override void OnPacket(Character pCharacter, byte pOpcode, Packet pPacket)
        {
            switch (pOpcode)
            {
                case 13:
                    {
                        byte charslot = pCharacter.RoomSlotId;
                        // Put Item
                        if (!IsFull())
                        {
                            // You can't put items while the second char isn't there yet
                            InventoryPacket.NoChange(pCharacter);
                            return;
                        }

                        byte inventory = pPacket.ReadByte();
                        short slot = pPacket.ReadShort();
                        short amount = pPacket.ReadShort();
                        byte toslot = pPacket.ReadByte();

                        var demItem = pCharacter.Inventory.GetItem(inventory, slot);

                        if (demItem == null || toslot < 1 || toslot > 9) // Todo: trade check
                        {
                            // HAX
                            var msg = $"Player tried to add an item in trade with to an incorrect slot. Item = null? {demItem == null}; toSlot {toslot}";
                            Program.MainForm.LogAppend(msg);
                            ReportManager.FileNewReport(msg, pCharacter.ID, 0);
                            InventoryPacket.NoChange(pCharacter);
                            return;
                        }

                        BaseItem tehItem = pCharacter.Inventory.TakeItemAmountFromSlot(inventory, slot, amount, Constants.isRechargeable(demItem.ItemID));

                        if (ItemList[charslot][toslot] == null)
                        {
                            ItemList[charslot][toslot] = new TradeItem()
                            {
                                OriginalItem = tehItem
                            };
                        }
                        
                        var pTradeItem = ItemList[charslot][toslot].OriginalItem;

                        ItemTransfer.PlayerTradePutUp(pCharacter.ID, demItem.ItemID, slot, amount, _transaction, demItem);

                        bool isUser0 = pCharacter.Name == Users[0].Name;

                        TradePacket.AddItem(Users[0], toslot, pTradeItem, (byte)(isUser0 ? 0 : 1));
                        TradePacket.AddItem(Users[1], toslot, pTradeItem, (byte)(isUser0 ? 1 : 0));

                        InventoryPacket.NoChange(pCharacter); // -.-
                        break;
                    }

                case 14: // Put mesos
                    {
                        //MessagePacket.SendNotice("PUTMESO PACKET: " + pPacket.ToString(), pCharacter);
                        int amount = pPacket.ReadInt();

                        if (amount < 0 || pCharacter.Inventory.Mesos < amount)
                        {
                            // HAX
                            var msg = "Player tried putting an incorrect meso amount in trade. Amount: " + amount;
                            Program.MainForm.LogAppend(msg);
                            ReportManager.FileNewReport(msg, pCharacter.ID, 0);
                            return;
                        }

                        pCharacter.AddMesos(-amount, true);
                        MesosTransfer.PlayerTradePutUp(pCharacter.ID, amount, _transaction);
                        Mesos[pCharacter.RoomSlotId] += amount;


                        bool isUser0 = pCharacter.Name == Users[0].Name;

                        TradePacket.PutCash(Users[0], Mesos[pCharacter.RoomSlotId], (byte)(isUser0 ? 0 : 1));
                        TradePacket.PutCash(Users[1], Mesos[pCharacter.RoomSlotId], (byte)(isUser0 ? 1 : 0));

                        break;
                    }

                // Accept trade
                case 0xF:
                    {
                        byte charslot = pCharacter.RoomSlotId;
                        Locked[charslot] = true;

                        for (int i = 0; i < 2; i++)
                        {
                            Character chr = Users[i];

                            if (chr != pCharacter)
                            {
                                TradePacket.SelectTrade(chr);
                            }
                        }

                        if (Locked[0] == true && Locked[1] == true)
                        {
                            Character chr = Users[0];
                            Character chr2 = Users[1];
                            if (ContinueTrade())
                            {
                                CompleteTrade();

                                TradePacket.TradeSuccessful(chr);
                                TradePacket.TradeSuccessful(chr2);
                            }
                            else
                            {
                                // Unsuccessful error
                                RemovePlayer(chr, 6);
                                RemovePlayer(chr2, 6);
                            }
                        }
                        break;
                    }
            }
        }
    }
}