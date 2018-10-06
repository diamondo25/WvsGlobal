using System;
using System.Linq;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;
using WvsBeta.Common.Tracking;
using WvsBeta.SharedDataProvider;

namespace WvsBeta.Game
{
    public static class StoragePacket
    {

        public enum StorageErrors
        {
            InventoryFullOrNot = 8, // Please check if your inventory is full or not
            NotEnoughMesos = 11, // You have not enough mesos. (Yes, that's a typo).
            StorageIsFull = 12, // The storage is full.
            DueToAnError = 13, // Due to an error, the trade did not happen.
        }

        public enum StorageEncode
        {
            EncodeMesos = 2,
            EncodeWithdraw = 7,
            EncodeDeposit = 9
        }

        public enum StorageAction
        {
            Withdraw = 3,
            Deposit = 4,
            StoreMesos = 5,
            Exit = 6
        }

        [Flags]
        public enum StorageEncodeFlags
        {
            EncodeMesos = 0x02,
            EncodeInventoryEquip = 0x04,
            EncodeInventoryUse = 0x08,
            EncodeInventorySetUp = 0x10,
            EncodeInventoryEtc = EncodeInventoryEquip, // FIX for old versions, put in 0x20 when client is fixed
            EncodeInventoryPet = 0x40, // Cash in newer versions

            EncodeAll = EncodeMesos | EncodeInventoryEquip | EncodeInventoryUse | EncodeInventorySetUp | EncodeInventoryEtc | EncodeInventoryPet,
        }

        public static void HandleStorage(Character chr, Packet pr)
        {
            if (chr.TrunkNPCID == 0) return;

            byte opcode = pr.ReadByte();
            switch ((StorageAction)opcode)
            {
                case StorageAction.Withdraw: // Remove
                    {
                        byte inventory = pr.ReadByte();
                        byte slot = pr.ReadByte();
                        BaseItem item = chr.Storage.GetItem(inventory, slot);
                        if (item == null)
                        {
                            return;
                        }

                        short amount = item.Amount;
                        if (!Constants.isStackable(item.ItemID))
                        {
                            amount = 1; // 1 'set'
                        }

                        if (chr.Inventory.HasSlotsFreeForItem(item.ItemID, amount, inventory != 1))
                        {
                            // AddItem2 will distribute stackable items
                            chr.Inventory.AddItem2(item);
                            chr.Storage.TakeItemOut(inventory, slot);

                            ItemTransfer.PlayerStorageWithdraw(chr.ID, chr.TrunkNPCID, item.ItemID, item.Amount, null, item);

                            EncodeStorage(chr, StorageEncode.EncodeWithdraw, GetEncodeFlagForInventory(Constants.getInventory(item.ItemID)));
                        }
                        else
                        {
                            SendError(chr, StorageErrors.InventoryFullOrNot);
                        }
                        break;
                    }
                case StorageAction.Deposit: // Add
                    {
                        byte slot = (byte)pr.ReadShort();
                        int itemid = pr.ReadInt();
                        short amount = pr.ReadShort();
                        NPCData data = DataProvider.NPCs[chr.TrunkNPCID];
                        var storageCost = data.Trunk;
                        if (chr.Inventory.Mesos < storageCost)
                        {
                            SendError(chr, StorageErrors.NotEnoughMesos);
                            return;
                        }

                        byte inventory = Constants.getInventory(itemid);
                        BaseItem item = chr.Inventory.GetItem(inventory, slot);
                        if (item == null || item.ItemID != itemid || item.CashId != 0)
                        {
                            // hax
                            return;
                        }
                        if (!chr.Storage.SlotsAvailable())
                        {
                            SendError(chr, StorageErrors.StorageIsFull);
                            return;
                        }

                        var isRechargable = Constants.isRechargeable(item.ItemID);
                        if (isRechargable) amount = item.Amount;

                        var possibleNewItem = chr.Inventory.TakeItemAmountFromSlot(inventory, slot, amount, true);
                        if (chr.AssertForHack(possibleNewItem == null, "Storage hack (amount > item.amount)")) return;

                        chr.Storage.AddItem(possibleNewItem);

                        ItemTransfer.PlayerStorageStore(chr.ID, chr.TrunkNPCID, item.ItemID, item.Amount, "" + item.GetHashCode() + " " + possibleNewItem.GetHashCode(), possibleNewItem);

                        EncodeStorage(chr, StorageEncode.EncodeDeposit, GetEncodeFlagForInventory(Constants.getInventory(item.ItemID)));
                        
                        chr.AddMesos(-storageCost); //why did you forget this diamondo :P
                        
                        MesosTransfer.PlayerGaveToNPC(chr.ID, chr.TrunkNPCID, storageCost, "" + item.GetHashCode());
                        break;
                    }
                case StorageAction.StoreMesos:
                    {
                        int mesos = pr.ReadInt();
                        if (mesos < 0)
                        {
                            // Store
                            if (chr.AssertForHack(Math.Abs(mesos) > chr.Inventory.Mesos, "Trying to store more mesos than he has") == false)
                            {
                                Common.Tracking.MesosTransfer.PlayerStoreMesos(chr.ID, Math.Abs(mesos));
                                chr.AddMesos(mesos);
                                chr.Storage.ChangeMesos(mesos);
                            }
                        }
                        else
                        {
                            // Withdraw
                            if (chr.AssertForHack(Math.Abs(mesos) > chr.Storage.Mesos, "Trying to withdraw more mesos than he has") == false)
                            {
                                Common.Tracking.MesosTransfer.PlayerRetrieveMesos(chr.ID, Math.Abs(mesos));
                                chr.AddMesos(mesos);
                                chr.Storage.ChangeMesos(mesos);
                            }

                        }
                        break;
                    }
                case StorageAction.Exit:
                    {
                        chr.TrunkNPCID = 0;
                        break;
                    }
                default:
                    {
                        Program.MainForm.LogAppend("Unknown Storage action: {0}", pr);
                        break;
                    }
            }
        }

        public static void SendShowStorage(Character chr, int NPCID)
        {
            Packet pw = new Packet(ServerMessages.STORAGE);
            pw.WriteInt(NPCID);

            EncodeStorage(chr, pw, StorageEncodeFlags.EncodeAll);

            chr.SendPacket(pw);
        }

        public static void SendChangedMesos(Character chr)
        {
            Packet pw = new Packet(ServerMessages.STORAGE_RESULT);
            pw.WriteByte(14);
            EncodeStorage(chr, pw, StorageEncodeFlags.EncodeMesos);
            chr.SendPacket(pw);
        }


        public static void SendError(Character chr, StorageErrors what)
        {
            Packet pw = new Packet(ServerMessages.STORAGE_RESULT);
            pw.WriteByte((byte)what);
            chr.SendPacket(pw);
        }

        public static void EncodeStorage(Character chr, StorageEncode enc, StorageEncodeFlags flags)
        {
            Packet packet = new Packet(ServerMessages.STORAGE_RESULT);
            packet.WriteByte((byte)enc);
            EncodeStorage(chr, packet, flags);
            chr.SendPacket(packet);
        }

        private static StorageEncodeFlags GetEncodeFlagForInventory(byte inventory)
        {
            StorageEncodeFlags flag;
            switch (inventory)
            {
                case 1: flag = StorageEncodeFlags.EncodeInventoryEquip; break;
                case 2: flag = StorageEncodeFlags.EncodeInventoryUse; break;
                case 3: flag = StorageEncodeFlags.EncodeInventorySetUp; break;
                case 4: flag = StorageEncodeFlags.EncodeInventoryEtc; break;
                case 5: flag = StorageEncodeFlags.EncodeInventoryPet; break;
                default: flag = 0; break;
            }
            return flag;
        }

        private static void EncodeStorage(Character chr, Packet packet, StorageEncodeFlags flags)
        {
            packet.WriteByte(chr.Storage.MaxSlots);

            packet.WriteShort((short)flags);

            if (flags.HasFlag(StorageEncodeFlags.EncodeMesos))
                packet.WriteInt(chr.Storage.Mesos);

            for (byte i = 1; i <= 5; i++)
            {
                StorageEncodeFlags flag = GetEncodeFlagForInventory(i);
                if (flags.HasFlag(flag))
                {
                    AddInvItems(chr, packet, i);
                }
            }
        }

        public static void AddInvItems(Character chr, Packet pw, byte inv)
        {
            var itemsInInventory = chr.Storage.GetInventoryItems(inv).ToArray();
            pw.WriteByte((byte)itemsInInventory.Length);

            foreach (var item in itemsInInventory)
            {
                BasePacketHelper.AddItemData(pw, item, 0, false);
            }
        }
    }
}