using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using log4net;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;
using WvsBeta.Common.Tracking;

namespace WvsBeta.Game
{
    public static class InventoryPacket
    {
        public static void HandleUseItemPacket(Character chr, Packet packet)
        {
            if (chr.PrimaryStats.HP < 1)
            {
                NoChange(chr);
                return;
            }

            short slot = packet.ReadShort();
            int itemid = packet.ReadInt();

            BaseItem item = chr.Inventory.GetItem(2, slot);
            if (item == null || item.ItemID != itemid || !DataProvider.Items.TryGetValue(itemid, out ItemData data))
            {
                return;
            }

            if (data.HP > 0)
            {
                chr.ModifyHP(data.HP);
            }
            if (data.MP > 0)
            {
                chr.ModifyMP(data.MP);
            }
            if (data.HPRate > 0)
            {
                chr.ModifyHP((short)(data.HPRate * chr.PrimaryStats.GetMaxHP(false) / 100), true);
            }
            if (data.MPRate > 0)
            {
                chr.ModifyMP((short)(data.MPRate * chr.PrimaryStats.GetMaxMP(false) / 100), true);
            }

            if (data.BuffTime > 0 || data.Cures != 0)
            {
                chr.Buffs.AddItemBuff(itemid);
            }

            bool delete = false;
            if (item.Amount <= 1)
            {
                item = null;
                delete = true;
            }
            else
            {
                item.Amount -= 1;
            }
            chr.Inventory.SetItem(2, slot, item);

            if (chr.PrimaryStats.BuffSpeed.R == itemid)
            {
                MapPacket.SendAvatarModified(chr, MapPacket.AvatarModFlag.Speed);
            }

            if (delete) // possible location of the switching to USE inventory bug - Joren
            {
                chr.Inventory.SetItem(2, slot, null);
                SwitchSlots(chr, slot, 0, 2);
            }
            else
            {
                AddItem(chr, 2, item, false);
            }
        }

        private static void DropItem(Character chr, byte inventory, short slot, short quantity)
        {
            if (chr.AssertForHack(chr.Room != null, "Trying to drop item while in a 'room'"))
            {
                NoChange(chr);
                return;
            }

            // Remove items from slot, taking stars if possible
            var drop = chr.Inventory.TakeItemAmountFromSlot(inventory, slot, quantity, true);

            if (drop == null)
            {
                // Item not found or quantity not enough
                NoChange(chr);
                return;
            }

            bool droppedFromEquips = Constants.isEquip(drop.ItemID) && slot < 0;

            ItemTransfer.ItemDropped(chr.ID, chr.MapID, drop.ItemID, drop.Amount, "", drop);

            chr.Field.DropPool.Create(Reward.Create(drop), chr.ID, 0, DropType.Normal, chr.ID, new Pos(chr.Position), chr.Position.X, 0, false, (short)(chr.Field.DropPool.DropEverlasting ? drop.InventorySlot : 0), false, true);

            if (droppedFromEquips)
            {
                MapPacket.SendAvatarModified(chr, MapPacket.AvatarModFlag.Equips);
            }
        }

        private static void ChangeSlot(Character chr, BaseItem from, BaseItem to, short slotFrom, short slotTo)
        {
            if (to != null)
            {
                if (
                    Constants.isStackable(to.ItemID) &&
                    to.ItemID == from.ItemID &&
                    // Do not allow cashitem stacking
                    to.CashId == 0 &&
                    from.CashId == 0
                )
                {
                    StackItems(chr, from, to, slotFrom, slotTo);
                    return;
                }
            }
            byte inventory = Constants.getInventory(@from.ItemID);

            chr.Inventory.SetItem(inventory, slotFrom, to);
            chr.Inventory.SetItem(inventory, slotTo, from);

            SwitchSlots(chr, slotFrom, slotTo, inventory);
            MapPacket.SendAvatarModified(chr, MapPacket.AvatarModFlag.Equips);
        }

        private static void StackItems(Character chr, BaseItem from, BaseItem to, short slotFrom, short slotTo)
        {
            short slotMax = (short)DataProvider.Items[from.ItemID].MaxSlot;
            if (slotMax == 0)
            {
                slotMax = 100;
            }
            byte inventory = Constants.getInventory(from.ItemID);

            if (to.Amount <= slotMax && to.Amount > 0) //adding to stack
            {
                short amount = to.Amount;
                short leftover = (short)(slotMax - amount);
                if (leftover < from.Amount)
                {
                    to.Amount += leftover;
                    from.Amount -= leftover;
                    AddItem2(chr, inventory, to, false, to.Amount);
                    AddItem2(chr, inventory, from, false, from.Amount);
                }
                else if (leftover >= from.Amount)
                {
                    to.Amount += from.Amount;
                    AddItem2(chr, inventory, to, false, to.Amount);
                    chr.Inventory.TakeItemAmountFromSlot(inventory, slotFrom, from.Amount, false);
                    NoChange(chr);
                }

            }
        }

        private static void EquipSpecial(Character chr, BaseItem from, BaseItem swordOrTop, short slotTo, bool unequipTwo = false)
        {
            byte inventory = Constants.getInventory(from.ItemID);
            if (unequipTwo) // If it's 2h Weapon or Overall, try to unequip both Shield + Weapon or Bottom + Top
            {
                BaseItem overallOr2h = from;
                BaseItem bottomOrShield = Constants.is2hWeapon(from.ItemID) ? chr.Inventory.GetItem(inventory, -10) : chr.Inventory.GetItem(inventory, -6);

                if (bottomOrShield != null)
                {
                    if (swordOrTop != null)
                    {
                        if (!chr.Inventory.HasSlotsFreeForItem(bottomOrShield.ItemID, 1, false))
                        {
                            NoChange(chr);
                            return;
                        }
                    }
                    ChangeSlot(chr, overallOr2h, swordOrTop, overallOr2h.InventorySlot, slotTo);
                    Unequip(chr, bottomOrShield, chr.Inventory.GetNextFreeSlotInInventory(inventory));
                    return;
                }
            }
            else // If it's Bottom or Shield, check if an Overall or 2h Weapon is equipped.
            {
                BaseItem bottomOrShield = from;
                BaseItem overallOr2h = Constants.isShield(from.ItemID) ? chr.Inventory.GetItem(inventory, -11) : chr.Inventory.GetItem(inventory, -5);
                if (overallOr2h != null)
                {
                    if (Constants.is2hWeapon(overallOr2h.ItemID) || Constants.isOverall(overallOr2h.ItemID))
                    {
                        ChangeSlot(chr, from, null, from.InventorySlot, slotTo);
                        Unequip(chr, overallOr2h, chr.Inventory.GetNextFreeSlotInInventory(inventory));
                        return;
                    }
                }
            }
            Equip(chr, from, swordOrTop, from.InventorySlot, slotTo); // If there's no special action required, go through regular equip.
        }

        private static void Equip(Character chr, BaseItem from, BaseItem to, short slotFrom, short slotTo)
        {
            byte inventory = Constants.getInventory(from.ItemID);
            chr.Inventory.SetItem(inventory, slotFrom, to);
            chr.Inventory.SetItem(inventory, slotTo, from);
            SwitchSlots(chr, slotFrom, slotTo, inventory);
            MapPacket.SendAvatarModified(chr, MapPacket.AvatarModFlag.Equips);
        }

        private static bool canWearItem(Character chr, CharacterPrimaryStats stats, EquipData data, short slot)
        {
            // Non-cash item on cash item slot
            if (!data.Cash && slot > 100)
                return false;
            // Cash item on non-cash item slot
            else if (data.Cash && slot > 0 && slot < 100)
                return false;
            else if (!Constants.IsCorrectBodyPart(data.ID, data.Cash ? (short)(slot - 100) : slot, chr.Gender))
                return false;
            else
                return stats.getTotalStr() >= data.RequiredStrength
                    && stats.getTotalDex() >= data.RequiredDexterity
                    && stats.getTotalInt() >= data.RequiredIntellect
                    && stats.getTotalLuk() >= data.RequiredLuck
                    && (stats.Fame >= data.RequiredFame || data.RequiredFame == 0)
                    && stats.Level >= data.RequiredLevel
                    && isRequiredJob(Constants.getJobTrack(stats.Job), data.RequiredJob);
        }

        [Flags]
        public enum JobFlags
        {
            Beginner = 0x00,
            Warrior = 0x01,
            Mage = 0x02,
            Archer = 0x04,
            Thief = 0x08
        }

        private static bool isRequiredJob(short jobTrack, ushort reqJob)
        {
            JobFlags job = 0;
            switch (jobTrack)
            {
                case 0: job = JobFlags.Beginner; break;
                case 1: job = JobFlags.Warrior; break;
                case 2: job = JobFlags.Mage; break;
                case 3: job = JobFlags.Archer; break;
                case 4: job = JobFlags.Thief; break;
            }
            return reqJob == 0 || ((short)job & reqJob) > 0;
        }

        private static void HandleEquip(Character chr, BaseItem from, BaseItem to, short slotFrom, short slotTo)
        {
            if (chr.AssertForHack(!canWearItem(chr, chr.PrimaryStats, DataProvider.Equips[from.ItemID], (short)-slotTo),
                $"Trying to wear an item that he cannot. from {slotFrom} to {slotTo}. Itemid: {from.ItemID}"))
            {
                // This should be handled by the client unless data.wz is editted
                // Possible actions: Error message, D/C, Ban / Warning
                NoChange(chr);
                return;
            }

            if (Constants.isOverall(from.ItemID) || Constants.is2hWeapon(from.ItemID))
            {
                EquipSpecial(chr, from, to, slotTo, true);
            }
            else if (Constants.isBottom(from.ItemID) || Constants.isShield(from.ItemID))
            {
                EquipSpecial(chr, from, to, slotTo);
            }
            else
            {
                Equip(chr, from, to, slotFrom, slotTo);
            }
        }

        private static void Unequip(Character chr, BaseItem equip, short slotTo)
        {
            byte inventory = Constants.getInventory(equip.ItemID);
            short slotFrom = equip.InventorySlot;

            BaseItem swap = chr.Inventory.GetItem(inventory, slotTo);

            if (swap == null && !chr.Inventory.HasSlotsFreeForItem(equip.ItemID, 1, false))  // Client checks this for us, but in case of PE
            {
                NoChange(chr);
                return;
            }

            if (swap != null && slotFrom < 0)
            {
                HandleEquip(chr, swap, equip, slotTo, slotFrom);
                return;
            }

            chr.Inventory.SetItem(inventory, slotFrom, swap);
            chr.Inventory.SetItem(inventory, slotTo, equip);

            SwitchSlots(chr, slotFrom, slotTo, inventory);
            MapPacket.SendAvatarModified(chr, MapPacket.AvatarModFlag.Equips);

        }

        public static void HandleInventoryPacket(Character chr, Packet packet)
        {
            try
            {
                byte inventory = packet.ReadByte();
                short slotFrom = packet.ReadShort(); // Slot from
                short slotTo = packet.ReadShort(); // Slot to

                if (slotFrom == 0 || inventory < 0 || inventory > 5) goto no_op;

                Trace.WriteLine($"Trying to swap from {slotFrom} to {slotTo}, inventory {inventory}");
                if (slotFrom < 0) Trace.WriteLine("From: " + (Constants.EquipSlots.Slots)((-slotFrom) % 100));
                if (slotTo < 0) Trace.WriteLine("To: " + (Constants.EquipSlots.Slots)((-slotTo) % 100));

                var itemFrom = chr.Inventory.GetItem(inventory, slotFrom); // Item being moved
                var itemTo = chr.Inventory.GetItem(inventory, slotTo); // Item in target position, if any

                if (itemFrom == null) goto no_op; // Packet Editing, from slot contains no item.
                if (slotTo < 0 && slotFrom < 0) goto no_op; // Packet Editing, both target and source slots are in equip.

                if (slotFrom > 0 && slotTo < 0)
                {
                    Trace.WriteLine($"HandleEquip");
                    HandleEquip(chr, itemFrom, itemTo, slotFrom, slotTo);
                }
                else if (slotFrom < 0 && slotTo > 0)
                {
                    Trace.WriteLine($"Unequip");
                    Unequip(chr, itemFrom, slotTo);
                }
                else if (slotTo == 0)
                {
                    if (chr.IsGM && !chr.IsAdmin)
                    {
                        MessagePacket.SendAdminWarning(chr, "You cannot drop items.");
                        NoChange(chr);
                    }
                    else
                    {
                        var quantity = packet.ReadShort();
                        DropItem(chr, inventory, slotFrom, quantity);
                    }
                }
                else
                {
                    Trace.WriteLine($"Changing slot");
                    ChangeSlot(chr, itemFrom, itemTo, slotFrom, slotTo);
                }

                chr.PrimaryStats.CheckBoosters();

                // TO-DO: Pets + Rings

                return;
            }
            catch (Exception ex)
            {
                Program.MainForm.LogAppend("[{0}] Exception item movement handler: {1}", chr.ID, ex.ToString());
            }

            no_op:
            Trace.WriteLine($"Sending nochange!");
            NoChange(chr);
        }

        public static void HandleUseSummonSack(Character chr, Packet packet)
        {
            short slot = packet.ReadShort();
            int itemid = packet.ReadInt();

            BaseItem item = chr.Inventory.GetItem(2, slot);
            if (item == null || item.ItemID != itemid || !DataProvider.Items.TryGetValue(itemid, out ItemData data))
            {
                NoChange(chr);
                return;
            }

            if (data.Summons.Count == 0)
            {
                NoChange(chr);
                return;
            }



            if (chr.AssertForHack(
                chr.Inventory.TakeItemAmountFromSlot(Constants.getInventory(itemid), slot, 1, false) == null,
                "Tried to use summon sack while not having them (???)"
            ))
                return;

            foreach (var isi in data.Summons)
            {
                if (DataProvider.Mobs.ContainsKey(isi.MobID))
                {
                    if (Rand32.Next() % 100 < isi.Chance)
                    {
                        chr.Field.SpawnMobWithoutRespawning(isi.MobID, chr.Position, chr.Foothold, summonType: data.Type);
                    }
                }
                else
                {
                    Program.MainForm.LogAppend("Summon sack {0} has mobid that doesn't exist: {1}", itemid, isi.MobID);
                }
            }
            NoChange(chr);
        }

        public static void HandleUseReturnScroll(Character chr, Packet packet)
        {
            short slot = packet.ReadShort();
            int itemid = packet.ReadInt();

            BaseItem item = chr.Inventory.GetItem(2, slot);
            if (item == null || item.ItemID != itemid || !DataProvider.Items.TryGetValue(itemid, out ItemData data))
            {
                NoChange(chr);
                return;
            }

            if (data == null || data.MoveTo == 0)
            {
                NoChange(chr);
                return;
            }
            int map;
            if (data.MoveTo == Constants.InvalidMap || !DataProvider.Maps.ContainsKey(data.MoveTo))
            {
                map = chr.Field.ReturnMap;
            }
            else
            {
                map = data.MoveTo;
            }

            chr.Inventory.TakeItem(itemid, 1);

            chr.ChangeMap(map);
        }

        public static ILog _scrollingLog = LogManager.GetLogger("ScrollingLog");
        public struct ScrollResult
        {
            public int itemId { get; set; }
            public int scrollId { get; set; }
            public object scrollData { get; set; }
            public object itemData { get; set; }
            public bool succeeded { get; set; }
            public bool cursed { get; set; }
        }

        public static void HandleScrollItem(Character chr, Packet packet)
        {
            short scrollslot = packet.ReadShort();
            short itemslot = packet.ReadShort();

            if (itemslot < -100)
            {
                _scrollingLog.Warn($"Tried to scroll a cashequip on slot {scrollslot}");
                NoChange(chr);
                return;
            }

            BaseItem scroll = chr.Inventory.GetItem(2, scrollslot);
            EquipItem equip = (EquipItem)chr.Inventory.GetItem(1, itemslot);
            if (scroll == null ||
                equip == null ||
                Constants.itemTypeToScrollType(equip.ItemID) != Constants.getScrollType(scroll.ItemID) ||
                !DataProvider.Items.TryGetValue(scroll.ItemID, out ItemData scrollData)
                )
            {
                _scrollingLog.Warn($"Tried to use a scroll that didn't exist {scroll == null}, equip that didnt exist {equip == null}, scroll types that didnt match or no scroll data available.");
                NoChange(chr);
                return;
            }

            if (scrollData.ScrollSuccessRate == 0 || equip.Slots == 0)
            {
                _scrollingLog.Warn($"Tried to scroll, but the equip has no scrolls left {equip.Slots} or zero success rate {scrollData.ScrollSuccessRate}");
                NoChange(chr);
                return;
            }

            chr.Inventory.TakeItem(scroll.ItemID, 1);
            var chanceRoll = Rand32.Next() % 100;

            bool succeeded = false;
            bool cursed = false;

            if (chanceRoll < scrollData.ScrollSuccessRate)
            {
                equip.Str += scrollData.IncStr;
                equip.Dex += scrollData.IncDex;
                equip.Int += scrollData.IncInt;
                equip.Luk += scrollData.IncLuk;
                equip.HP += scrollData.IncMHP;
                equip.MP += scrollData.IncMMP;
                equip.Watk += scrollData.IncWAtk;
                equip.Wdef += scrollData.IncWDef;
                equip.Matk += scrollData.IncMAtk;
                equip.Mdef += scrollData.IncMDef;
                equip.Acc += scrollData.IncAcc;
                equip.Avo += scrollData.IncAvo;
                equip.Jump += scrollData.IncJump;
                equip.Speed += scrollData.IncSpeed;
                equip.Scrolls++;
                equip.Slots--;

                succeeded = true;

                AddItem(chr, 1, equip, true);
                MapPacket.SendAvatarModified(chr, MapPacket.AvatarModFlag.Equips);
                SendItemScrolled(chr, true);
            }
            else
            {
                if (chanceRoll < scrollData.ScrollCurseRate)
                {
                    cursed = true;
                    chr.Inventory.TryRemoveCashItem(equip);

                    SwitchSlots(chr, itemslot, 0, 1);
                    chr.Inventory.SetItem(1, itemslot, null);
                    SendItemScrolled(chr, false);

                    chr.PrimaryStats.CheckBoosters();
                }
                else
                {
                    equip.Slots--;
                    AddItem(chr, 1, equip, true);
                    SendItemScrolled(chr, false);
                }
            }

            _scrollingLog.Info(new ScrollResult
            {
                itemData = equip,
                itemId = equip.ItemID,
                scrollData = scrollData,
                scrollId = scroll.ItemID,
                succeeded = succeeded,
                cursed = cursed
            });
        }

        public static void SwitchSlots(Character chr, short slot1, short slot2, byte inventory)
        {
            Packet pw = new Packet(ServerMessages.INVENTORY_OPERATION);
            pw.WriteByte(0x01);
            pw.WriteByte(0x01);
            pw.WriteByte(0x02);
            pw.WriteByte(inventory);
            pw.WriteShort(slot1);
            pw.WriteShort(slot2);
            pw.WriteByte(0); // FIX: This should be the 'movement info index'
            chr.SendPacket(pw);
        }

        public static void MultiDelete(Character chr, byte inventory, params short[] slots)
        {
            const byte MaxSizePerPacket = 100;
            int slotsToWipe = slots.Length;
            for (var i = 0; i < slotsToWipe;)
            {
                var chunk = Math.Min(MaxSizePerPacket, slotsToWipe - i);
                Packet pw = new Packet(ServerMessages.INVENTORY_OPERATION);
                pw.WriteByte(0x01);
                pw.WriteByte((byte)chunk);
                for (var j = 0; j < chunk; j++)
                {
                    pw.WriteByte(0x02);
                    pw.WriteByte(inventory);
                    pw.WriteShort(slots[i + j]);
                    pw.WriteShort(0);
                }
                pw.WriteByte(0x00);
                chr.SendPacket(pw);

                i += chunk;
            }
        }

        public static void AddItem(Character chr, byte inventory, BaseItem item, bool isNew)
        {
            Packet pw = new Packet(ServerMessages.INVENTORY_OPERATION);
            pw.WriteByte(0x01);
            pw.WriteByte(0x01);
            pw.WriteBool(!isNew);
            pw.WriteByte(inventory);
            if (isNew)
            {
                PacketHelper.AddItemData(pw, item, item.InventorySlot, true);
            }
            else
            {
                pw.WriteShort(item.InventorySlot);
                pw.WriteShort(item.Amount);
            }
            pw.WriteLong(0x00);
            pw.WriteLong(0x00);
            pw.WriteLong(0x00);
            chr.SendPacket(pw);
        }

        public static void AddItem2(Character chr, byte inventory, BaseItem item, bool isNew, short amount)
        {
            Packet pw = new Packet(ServerMessages.INVENTORY_OPERATION);
            pw.WriteByte(0x01);
            pw.WriteByte(0x01);
            pw.WriteBool(!isNew);
            pw.WriteByte(inventory);
            if (isNew)
            {
                PacketHelper.AddItemData(pw, item, item.InventorySlot, true);
            }
            else
            {
                pw.WriteShort(item.InventorySlot);
                pw.WriteShort(amount);
            }
            pw.WriteLong(0x00);
            pw.WriteLong(0x00);
            pw.WriteLong(0x00);
            chr.SendPacket(pw);
        }

        public static void NoChange(Character chr)
        {
            Packet pw = new Packet(ServerMessages.INVENTORY_OPERATION);
            pw.WriteByte(0x01);
            pw.WriteByte(0x00);
            pw.WriteByte(0x00);
            chr.SendPacket(pw);
        }

        public static void IncreaseSlots(Character chr, byte inventory, byte amount)
        {
            Packet pw = new Packet(ServerMessages.INVENTORY_GROW);
            pw.WriteByte(inventory);
            pw.WriteByte(amount);
            pw.WriteLong(0);
            chr.SendPacket(pw);
        }

        public static void SendItemScrolled(Character chr, bool pSuccessfull)
        {
            Packet pw = new Packet(ServerMessages.SHOW_STATUS_INFO);
            pw.WriteByte(4);
            pw.WriteBool(pSuccessfull);
            chr.SendPacket(pw);
        }

        public static void InventoryFull(Character chr)
        {
            Packet packet = new Packet(ServerMessages.SHOW_STATUS_INFO);
            packet.WriteByte(0);
            packet.WriteByte(2);
            chr.SendPacket(packet);
        }

        public static void SendItemsExpired(Character chr, List<int> pExpiredItems) // "The item [name] has been expired, and therefore, deleted from your inventory." * items
        {
            if (pExpiredItems.Count == 0) return;
            const byte MaxSizePerPacket = 100;
            for (int i = 0; i < pExpiredItems.Count; i += MaxSizePerPacket)
            {
                int amount = Math.Min(MaxSizePerPacket, pExpiredItems.Count - i);

                Packet pw = new Packet(ServerMessages.SHOW_STATUS_INFO);
                pw.WriteByte(5);
                pw.WriteByte((byte)amount);

                foreach (var item in pExpiredItems.Skip(i).Take(amount))
                    pw.WriteInt(item);

                chr.SendPacket(pw);
            }
        }

        public static void SendCashItemExpired(Character chr, int pExpiredItem) // "The available time for the cash item [name] has passedand the item is deleted."
        {
            Packet pw = new Packet(ServerMessages.SHOW_STATUS_INFO);
            pw.WriteByte(2);
            pw.WriteInt(pExpiredItem);
            chr.SendPacket(pw);
        }
    }
}