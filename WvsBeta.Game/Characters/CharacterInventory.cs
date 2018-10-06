using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using MySql.Data.MySqlClient;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;
using WvsBeta.SharedDataProvider;

namespace WvsBeta.Game
{
    public class CharacterInventory : BaseCharacterInventory
    {
        private Character Character { get; set; }
        public int ChocoCount { get; private set; }
        public int ActiveItemID { get; private set; }

        public CharacterInventory(Character character) : base(character.UserID, character.ID)
        {
            Character = character;
        }

        public void SaveInventory()
        {
            base.SaveInventory(Program.MainForm.LogAppend);
        }

        public void LoadInventory()
        {
            base.LoadInventory();

            UpdateChocoCount(false);
        }

        public override void AddItem(byte inventory, short slot, BaseItem item, bool isLoading)
        {
            base.AddItem(inventory, slot, item, isLoading);

            if (slot < 0 && item is EquipItem equipItem)
            {
                slot = Math.Abs(slot);
                Character.PrimaryStats.AddEquipStats((sbyte)slot, equipItem, isLoading);
            }

            if (!isLoading)
                UpdateChocoCount();
        }

        public void SetItem(byte inventory, short slot, BaseItem item)
        {
            inventory -= 1;
            if (item != null) item.InventorySlot = slot;
            if (slot < 0)
            {

                EquipItem equipItem = item as EquipItem;
                slot = Math.Abs(slot);
                if (slot > 100)
                {
                    Equips[1][(byte)(slot - 100)] = equipItem;
                }
                else
                {
                    Equips[0][(byte)slot] = equipItem;
                    Character.PrimaryStats.AddEquipStats((sbyte)slot, equipItem, false);
                }
            }
            else
            {
                Items[inventory][slot] = item;
            }

            UpdateChocoCount();
        }

        public int GetEquippedItemId(Constants.EquipSlots.Slots slot, bool cash) => GetEquippedItemId((short)slot, cash);

        public int GetEquippedItemId(short slot, bool cash)
        {
            if (!cash)
            {
                slot = Math.Abs(slot);
                if (Equips[0].Length > slot)
                {
                    if (Equips[0][slot] != null)
                    {
                        return Equips[0][slot].ItemID;
                    }
                }
            }
            else
            {
                if (slot < -100)
                {
                    slot += 100;
                }
                slot = Math.Abs(slot);
                if (Equips[1].Length > slot)
                {
                    if (Equips[1][slot] != null)
                    {
                        return Equips[1][slot].ItemID;
                    }
                }
            }
            return 0;
        }

        public void UpdateChocoCount(bool sendPacket = true)
        {

            int prevChocoCount = ChocoCount;
            ChocoCount = Items[Constants.getInventory(Constants.Items.Choco) - 1].Count(x => x?.ItemID == Constants.Items.Choco);
            ActiveItemID = ChocoCount > 0 ? Constants.Items.Choco : 0;

            if (sendPacket && prevChocoCount != ChocoCount)
            {
                MapPacket.SendAvatarModified(Character, MapPacket.AvatarModFlag.ItemEffects);
            }
        }

        public int GetItemAmount(int itemid)
        {
            int amount = 0;
            BaseItem temp = null;


            for (byte inventory = 1; inventory <= 5; inventory++)
            {
                for (short i = 1; i <= MaxSlots[inventory - 1]; i++)
                { // Slot 1 - 24, not 0 - 23
                    temp = GetItem(inventory, i);
                    if (temp != null && temp.ItemID == itemid) amount += temp.Amount;
                }
            }

            return amount;
        }


        public short AddItem2(BaseItem item, bool sendpacket = true)
        {
            byte inventory = Constants.getInventory(item.ItemID);
            short slot = 0;
            // see if there's a free slot
            BaseItem temp = null;
            short maxSlots = 1;
            if (DataProvider.Items.TryGetValue(item.ItemID, out ItemData itemData))
            {
                maxSlots = (short)itemData.MaxSlot;
                if (maxSlots == 0)
                {
                    // 1, 100 or specified
                    maxSlots = 100;
                }
            }
            for (short i = 1; i <= MaxSlots[inventory - 1]; i++)
            { // Slot 1 - 24, not 0 - 23
                temp = GetItem(inventory, i);
                if (temp != null)
                {
                    if (Constants.isStackable(item.ItemID) && item.ItemID == temp.ItemID && temp.Amount < maxSlots)
                    {
                        if (item.Amount + temp.Amount > maxSlots)
                        {

                            short amount = (short)(maxSlots - temp.Amount);
                            item.Amount -= amount;
                            temp.Amount = maxSlots;
                            if (sendpacket)
                                InventoryPacket.AddItem(Character, inventory, temp, false);
                        }
                        else
                        {
                            item.Amount += temp.Amount;
                            // Removing the item looks a bit odd to me?
                            SetItem(inventory, i, null);
                            AddItem(inventory, i, item, false);
                            if (sendpacket)
                                InventoryPacket.AddItem(Character, inventory, item, false);
                            return 0;
                        }
                    }
                }
                else if (slot == 0)
                {
                    slot = i;
                    if (!Constants.isStackable(item.ItemID))
                    {
                        break;
                    }
                }
            }
            if (slot != 0)
            {
                SetItem(inventory, slot, item);
                if (sendpacket)
                    InventoryPacket.AddItem(Character, inventory, item, true);
                return 0;
            }
            else
            {
                return item.Amount;
            }
        }

        public short AddNewItem(int id, short amount) // Only normal items!
        {
            if (!DataProvider.Items.ContainsKey(id) &&
                !DataProvider.Equips.ContainsKey(id) &&
                !DataProvider.Pets.ContainsKey(id))
            {
                return 0;
            }

            short max = 1;
            if (!Constants.isEquip(id) && !Constants.isPet(id))
            {
                max = (short)DataProvider.Items[id].MaxSlot;
                if (max == 0)
                {
                    max = 100;
                }
            }
            short thisAmount = 0, givenAmount = 0;

            if (Constants.isRechargeable(id))
            {
                thisAmount = (short)(max + Character.Skills.GetRechargeableBonus());
                amount -= 1;
            }
            else if (Constants.isEquip(id) || Constants.isPet(id))
            {
                thisAmount = 1;
                amount -= 1;
            }
            else if (amount > max)
            {
                thisAmount = max;
                amount -= max;
            }
            else
            {
                thisAmount = amount;
                amount = 0;
            }

            if (Constants.isPet(id))
            {
                givenAmount = 0;
            }
            else
            {
                var item = BaseItem.CreateFromItemID(id);
                item.Amount = thisAmount;

                if (Constants.isEquip(id))
                {
                    item.GiveStats(ItemVariation.None);
                }
                givenAmount += thisAmount;
                if (AddItem2(item) == 0 && amount > 0)
                {
                    givenAmount += AddNewItem(id, amount);
                }
            }

            return givenAmount;
        }

        public bool HasSlotsFreeForItem(int itemid, short amount, bool stackable)
        {
            short slotsRequired = 0;
            byte inventory = Constants.getInventory(itemid);
            if (!Constants.isStackable(itemid) && !Constants.isStar(itemid))
            {
                slotsRequired = amount;
            }
            else if (Constants.isStar(itemid))
            {
                slotsRequired = 1;
            }
            else
            {
                short maxPerSlot = (short)DataProvider.Items[itemid].MaxSlot;
                if (maxPerSlot == 0) maxPerSlot = 100; // default 100 O.o >_>
                short amountAlready = (short)(ItemAmounts.ContainsKey(itemid) ? ItemAmounts[itemid] : 0);
                if (stackable && amountAlready > 0)
                {
                    // We should try to see which slots we can fill, and determine how much new slots are left

                    short amountLeft = amount;
                    byte inv = Constants.getInventory(itemid);
                    inv -= 1;
                    foreach (var item in Items[inv].ToList().FindAll(x => x != null && x.ItemID == itemid && x.Amount < maxPerSlot))
                    {
                        amountLeft -= (short)(maxPerSlot - item.Amount); // Substract the amount of 'slots' left for this slot
                        if (amountLeft <= 0)
                        {
                            amountLeft = 0;
                            break;
                        }
                    }

                    // Okay, so we need to figure out where to keep these stackable items.

                    // Apparently we've got space left on slots
                    if (amountLeft == 0) return true;

                    // Hmm, still need to get more slots
                    amount = amountLeft;
                }

                slotsRequired = (short)(amount / maxPerSlot);
                // Leftover slots to handle
                if ((amount % maxPerSlot) > 0)
                    slotsRequired++;

            }
            return GetOpenSlotsInInventory(inventory) >= slotsRequired;
        }

        public int ItemAmountAvailable(int itemid)
        {
            byte inv = Constants.getInventory(itemid);
            int available = 0;
            short maxPerSlot = (short)(DataProvider.Items.ContainsKey(itemid) ? DataProvider.Items[itemid].MaxSlot : 1); // equip
            if (maxPerSlot == 0) maxPerSlot = 100; // default 100 O.o >_>

            short openSlots = GetOpenSlotsInInventory(inv);
            available += (openSlots * maxPerSlot);

            BaseItem temp = null;

            for (short i = 1; i <= MaxSlots[inv - 1]; i++)
            {
                temp = GetItem(inv, i);
                if (temp != null && temp.ItemID == itemid)
                    available += (maxPerSlot - temp.Amount);
            }

            return available;
        }

        public short GetOpenSlotsInInventory(byte inventory)
        {
            short amount = 0;
            for (short i = 1; i <= MaxSlots[inventory - 1]; i++)
            {
                if (GetItem(inventory, i) == null)
                    amount++;
            }
            return amount;
        }

        public short GetNextFreeSlotInInventory(byte inventory)
        {
            for (short i = 1; i <= MaxSlots[inventory - 1]; i++)
            {
                if (GetItem(inventory, i) == null)
                    return i;
            }
            return -1;
        }

        public void GenerateInventoryPacket(Packet packet)
        {
            packet.WriteInt(Mesos);

            foreach (var item in Equips[0])
            {
                if (item == null) continue;
                PacketHelper.AddItemData(packet, item, item.InventorySlot, false);
            }

            packet.WriteByte(0);

            foreach (var item in Equips[1])
            {
                if (item == null) continue;
                PacketHelper.AddItemData(packet, item, item.InventorySlot, false);
            }
            packet.WriteByte(0);

            for (int i = 0; i < 5; i++)
            {
                packet.WriteByte(MaxSlots[i]);
                foreach (BaseItem item in Items[i])
                {
                    if (item != null && item.InventorySlot > 0)
                    {
                        PacketHelper.AddItemData(packet, item, item.InventorySlot, false);
                    }
                }

                packet.WriteByte(0);
            }
        }

        public short DeleteFirstItemInInventory(int inv)
        {
            for (short i = 1; i <= MaxSlots[inv]; i++)
            {
                if (Items[inv][i] != null)
                {
                    Items[inv][i] = null;
                    UpdateChocoCount();
                    return i;
                }
            }
            return 0;
        }

        /// <summary>
        /// Set the MaxSlots for <param name="inventory"/> to <param name="slots" />.
        /// If the Items array is already initialized, it will either expand the array,
        /// or, when <param name="slots" /> is less, will remove items and shrink it.
        /// </summary>
        /// <param name="inventory">Inventory ID, 0-5</param>
        /// <param name="slots">Amount of slots</param>
        public override void SetInventorySlots(byte inventory, byte slots, bool sendPacket = true)
        {
            base.SetInventorySlots(inventory, slots, sendPacket);

            if (sendPacket)
                InventoryPacket.IncreaseSlots(Character, inventory, slots);
        }

        /// <summary>
        /// Try to remove <paramref name="amount"/> amount of itemid <paramref name="itemid"/>.
        /// Does not 'remove' stacks, keeps them as-is (with 0 items).
        /// </summary>
        /// <param name="itemid">The Item ID</param>
        /// <param name="amount">Amount</param>
        /// <returns>Amount of items that were _not_ taken away</returns>
        public int TakeItem(int itemid, int amount)
        {
            if (amount == 0) return 0;

            int initialAmount = amount;
            var isRechargeable = Constants.isRechargeable(itemid);
            byte inventory = Constants.getInventory(itemid);
            for (short i = 1; i <= MaxSlots[inventory - 1]; i++)
            {
                BaseItem item = GetItem(inventory, i);
                if (item == null || item.ItemID != itemid) continue;

                var maxRemove = Math.Min(item.Amount, amount);
                item.Amount -= (short)maxRemove;
                if (item.Amount == 0 && !isRechargeable)
                {
                    // Your item. Gone.
                    SetItem(inventory, i, null);
                    TryRemoveCashItem(item);
                    InventoryPacket.SwitchSlots(Character, i, 0, inventory);
                }
                else
                {
                    // Update item with new amount
                    InventoryPacket.AddItem(Character, inventory, item, false);
                }
                amount -= maxRemove;
            }

            return initialAmount - amount;
        }

        public BaseItem TakeItemAmountFromSlot(byte inventory, short slot, short amount, bool takeStars)
        {
            var item = GetItem(inventory, slot);

            if (item == null) return null;

            if (!takeStars)
            {
                if (item.Amount - amount < 0) return null;
            }

            bool removeItem = false;
            BaseItem newItem;
            if (takeStars && Constants.isStar(item.ItemID))
            {
                // Take the whole item
                newItem = item;
                removeItem = true;
            }
            else
            {
                newItem = item.SplitInTwo(amount);
                removeItem = item.Amount == 0 && Constants.isStar(item.ItemID) == false;
            }

            if (removeItem)
            {
                SetItem(inventory, slot, null);
                TryRemoveCashItem(item);
                InventoryPacket.SwitchSlots(Character, slot, 0, inventory);
            }
            else
            {
                // Update item
                InventoryPacket.AddItem(Character, inventory, item, false);
            }

            return newItem;
        }

        public Dictionary<byte, int> GetVisibleEquips()
        {
            Dictionary<byte, int> shown = new Dictionary<byte, int>();


            foreach (var item in Equips[1])
            {
                if (item != null)
                {
                    byte slotuse = (byte)Math.Abs(item.InventorySlot);
                    if (slotuse > 100) slotuse -= 100;
                    shown.Add(slotuse, item.ItemID);
                }
            }

            foreach (var item in Equips[0])
            {
                if (item != null && !shown.ContainsKey((byte)Math.Abs(item.InventorySlot)))
                {
                    shown.Add((byte)Math.Abs(item.InventorySlot), item.ItemID);
                }
            }
            return shown;
        }

        public int GetTotalWAttackInEquips(bool star)
        {
            int totalWat = 0;

            foreach (var item in Equips[0])
            {
                if (item?.Watk > 0)
                {
                    totalWat += item.Watk;
                }
            }

            if (star == true)
            {
                foreach (BaseItem item in Items[1])
                {
                    if (item != null)
                    {
                        if (Constants.isStar(item.ItemID))
                        {
                            switch (item.ItemID)
                            {
                                case 2070000: totalWat += 15; break; // Subi Throwing Star +15
                                case 2070001:                        // Wolbi Throwing Star +17
                                case 2070008: totalWat += 17; break; // Snowball +17
                                case 2070002:                        // Mokbi Throwing Star +19
                                case 2070009: totalWat += 19; break; // Top +19
                                case 2070012: totalWat += 20; break; // Paper Airplane +20
                                case 2070003:                        // Kumbi Throwing Star +21
                                case 2070010:                        // Icicle +21
                                case 2070011: totalWat += 21; break; // Maple Throwing Star +21
                                case 2070004: totalWat += 23; break; // Tobi Throwing Star +23
                                case 2070005: totalWat += 25; break; // Steely Throwing Star +25
                                case 2070006:                        // Ilbi Throwing Star +27
                                case 2070007: totalWat += 27; break; // Hwabi Throwing Star +27
                            }

                            break;
                        }

                        else if (Constants.isArrow(item.ItemID))
                        {
                            switch (item.ItemID)
                            {
                                case 2060000:                       // Arrow For Bow
                                case 2061000: break;                // Arrow for Crossbow
                                case 2060001:                       // Bronze Arrow for Bow
                                case 2061001:                       // Bronze Arrow for Crossbow
                                case 2060002:                       // Steel Arrow for Bow
                                case 2061002: totalWat += 1; break; // Steel Arrow for Crossbow
                            }
                        }
                    }
                }
            }

            return totalWat;
        }

        public int GetTotalMAttInEquips()
        {
            return Equips[0]
                .Where(i => i != null)
                .Sum(item => item.Matk);
        }

        public int GetTotalAccInEquips()
        {
            return Equips[0]
                .Where(i => i != null)
                .Sum(item => item.Acc);
        }

        public double GetExtraExpRate()
        {
            // Holiday stuff here.
            double rate = 1;

            foreach (BaseItem item in this.Items[3])
            {
                if (item == null || item.ItemID < 4100000 || item.ItemID >= 4200000) continue;
                ItemData id = DataProvider.Items[item.ItemID];
                if (ItemData.RateCardEnabled(id, false))
                {
                    if (rate < id.Rate) rate = id.Rate;
                }
            }
            return rate;
        }


        private long lastCheck = 0;
        public void GetExpiredItems(long time, Action<List<BaseItem>> callback)
        {
            if (time - lastCheck < 45000) return;
            lastCheck = time;

            var allItems = Equips[0]
                .Concat(Equips[1])
                .Concat(Items[0])
                .Concat(Items[1])
                .Concat(Items[2])
                .Concat(Items[3])
                .Concat(Items[4])
                .Where(x =>
                    x != null &&
                    x.Expiration < time
                )
                .ToList();

            if (allItems.Count == 0) return;

            callback(allItems);
        }


        public void CheckExpired()
        {
            var currentTime = MasterThread.CurrentDate.ToFileTimeUtc();
            _cashItems.GetExpiredItems(currentTime, expiredItems =>
            {
                var dict = new Dictionary<byte, List<short>>();
                expiredItems.ForEach(x =>
                {
                    InventoryPacket.SendCashItemExpired(Character, x.ItemId);
                    var inventory = Constants.getInventory(x.ItemId);
                    var baseItem = GetItemByCashID(x.CashId, inventory);

                    if (baseItem != null)
                    {
                        if (dict.TryGetValue(inventory, out var curList)) curList.Add(baseItem.InventorySlot);
                        else
                        {
                            dict[inventory] = new List<short> { baseItem.InventorySlot };
                        }
                    }
                    RemoveLockerItem(x, baseItem, true);
                });

                dict.ForEach(x => InventoryPacket.MultiDelete(Character, x.Key, x.Value.ToArray()));
            });

            GetExpiredItems(currentTime, expiredItems =>
            {
                var dict = new Dictionary<byte, List<short>>();
                var itemIds = new List<int>();
                expiredItems.ForEach(x =>
                {
                    var inventory = Constants.getInventory(x.ItemID);
                    if (x.CashId != 0)
                    {
                        var baseItem = GetItemByCashID(x.CashId, inventory);
                        if (dict.TryGetValue(inventory, out var curList)) curList.Add(baseItem.InventorySlot);
                        else
                        {
                            dict[inventory] = new List<short> {baseItem.InventorySlot};
                        }
                        TryRemoveCashItem(x);
                    }
                    SetItem(inventory, x.InventorySlot, null);
                    itemIds.Add(x.ItemID);
                });

                InventoryPacket.SendItemsExpired(Character, itemIds);
                dict.ForEach(x => InventoryPacket.MultiDelete(Character, x.Key, x.Value.ToArray()));
            });
        }
    }
}