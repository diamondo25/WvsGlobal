using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;
using WvsBeta.Database;
using WvsBeta.Game;

namespace WvsBeta.SharedDataProvider
{
    public abstract class BaseCharacterInventory
    {
        // Shown and hidden
        protected EquipItem[][] Equips { get; } =
        {
            new EquipItem[17],
            new EquipItem[120] // Pet equips
        };

        // All inventories
        protected BaseItem[][] Items { get; } = new BaseItem[5][];

        protected Dictionary<int, short> ItemAmounts { get; } = new Dictionary<int, short>();
        public byte[] MaxSlots { get; } = new byte[5];
        protected int[] TeleportRockLocations { get; } = new int[5];

        public int Mesos { get; set; }

        public static MySQL_Connection Connection { get; set; }
        protected CharacterCashItems _cashItems;

        private int UserID { get; }
        private int CharacterID { get; }

        protected BaseCharacterInventory(int userId, int characterId)
        {
            UserID = userId;
            CharacterID = characterId;
            _cashItems = new CharacterCashItems(UserID, CharacterID);
        }


        protected void LoadInventory()
        {
            using (var data = Connection.RunQuery("SELECT mesos, equip_slots, use_slots, setup_slots, etc_slots, cash_slots FROM characters WHERE id = " + CharacterID) as MySqlDataReader)
            {
                data.Read();

                Mesos = data.GetInt32("mesos");
                SetInventorySlots(1, (byte)data.GetInt16("equip_slots"), false);
                SetInventorySlots(2, (byte)data.GetInt16("use_slots"), false);
                SetInventorySlots(3, (byte)data.GetInt16("setup_slots"), false);
                SetInventorySlots(4, (byte)data.GetInt16("etc_slots"), false);
                SetInventorySlots(5, (byte)data.GetInt16("cash_slots"), false);
            }

            SplitDBInventory.Load(Connection, "inventory", "charid = " + CharacterID, (type, inventory, slot, item) =>
            {
                AddItem(inventory, slot, item, true);
            });

            _cashItems.Load();

            // Move items over to the inventory

            foreach (var cashItemsEquip in _cashItems.Equips)
            {
                Console.WriteLine("Adding cash equip on slot {0}", cashItemsEquip.InventorySlot);
                AddItem(1, cashItemsEquip.InventorySlot, cashItemsEquip, true);
            }

            foreach (var cashItemsBundle in _cashItems.Bundles)
            {
                Console.WriteLine("Adding bundle on slot {0}", cashItemsBundle.InventorySlot);
                AddItem(Constants.getInventory(cashItemsBundle.ItemID), cashItemsBundle.InventorySlot, cashItemsBundle, true);
            }
            
            foreach (var cashItemPets in _cashItems.Pets)
            {
                Console.WriteLine("Adding pet on slot {0}", cashItemPets.InventorySlot);
                AddItem(Constants.getInventory(cashItemPets.ItemID), cashItemPets.InventorySlot, cashItemPets, true);
            }

            _cashItems.Equips.Clear();
            _cashItems.Bundles.Clear();
            _cashItems.Pets.Clear();

            using (var data = Connection.RunQuery("SELECT mapindex, mapid FROM teleport_rock_locations WHERE charid = " + CharacterID) as MySqlDataReader)
            {
                while (data.Read())
                {
                    TeleportRockLocations[data.GetByte("mapindex")] = data.GetInt32("mapid");
                }
            }

            for (int i = 0; i < TeleportRockLocations.Length; i++)
                if (TeleportRockLocations[i] == 0)
                    TeleportRockLocations[i] = Constants.InvalidMap;
        }

        public void SaveCashItems(CharacterCashItems otherItems)
        {
            // Move cashitems back to the _cashItems object

            // 'Hidden' equips
            _cashItems.Equips.AddRange(Equips[1].Where(y => y != null && y.CashId != 0));
            // Unequipped equips
            _cashItems.Equips.AddRange(Items[0].Where(y => y is EquipItem && y.CashId != 0).Select(y => y as EquipItem));
            // Bundles
            _cashItems.Bundles.AddRange(Items.SelectMany(x => x.Where(y => y is BundleItem && y.CashId != 0).Select(y => y as BundleItem)));
            // Pets
            _cashItems.Pets.AddRange(Items.SelectMany(x => x.Where(y => y is PetItem && y.CashId != 0).Select(y => y as PetItem)));

            if (otherItems == null)
                CharacterCashItems.SaveMultiple(_cashItems);
            else
                CharacterCashItems.SaveMultiple(_cashItems, otherItems);

            // Cleanup for next save
            _cashItems.Equips.Clear();
            _cashItems.Bundles.Clear();
            _cashItems.Pets.Clear();
        }


        protected void SaveInventory(MySQL_Connection.LogAction dbgCallback = null)
        {

            string query = "UPDATE characters SET " +
                           "mesos = " + Mesos + " ," +
                           "equip_slots = " + MaxSlots[0] + ", " +
                           "use_slots = " + MaxSlots[1] + ", " +
                           "setup_slots = " + MaxSlots[2] + ", " +
                           "etc_slots = " + MaxSlots[3] + ", " +
                           "cash_slots = " + MaxSlots[4] + " " +
                           "WHERE ID = " + CharacterID;

            Connection.RunTransaction(query, dbgCallback);

            Connection.RunTransaction(comm =>
            {
                comm.CommandText = "DELETE FROM teleport_rock_locations WHERE charid = " + CharacterID;
                comm.ExecuteNonQuery();

                var telerockSave = new StringBuilder();
                telerockSave.Append("INSERT INTO teleport_rock_locations VALUES ");
                int idx = 0;
                telerockSave.Append(string.Join(", ", TeleportRockLocations.Select(location => "(" + CharacterID + ", " + (idx++) + ", " + location + ")")));
                comm.CommandText = telerockSave.ToString();
                comm.ExecuteNonQuery();
            }, dbgCallback);


            SplitDBInventory.Save(
                Connection,
                "inventory",
                CharacterID + ", ",
                "charid = " + CharacterID,
                (type, inventory) =>
                {
                    switch (type)
                    {
                        case SplitDBInventory.InventoryType.Eqp:
                            return Equips.SelectMany(x => x.Where(y => y != null && y.CashId == 0)).Union(Items[0].Where(x => x != null && x.CashId == 0));
                        case SplitDBInventory.InventoryType.Bundle:
                            return Items[inventory - 1].Where(x => x != null && x.CashId == 0);
                        default: throw new Exception();
                    }
                },
                dbgCallback
            );
        }

        public void TryRemoveCashItem(BaseItem item)
        {
            var lockerItem = GetLockerItemByCashID(item.CashId);
            if (lockerItem != null)
            {
                RemoveLockerItem(lockerItem, item, true);
            }
        }

        public void AddLockerItem(LockerItem item)
        {
            _cashItems.Items.Add(item);
        }

        public void RemoveLockerItem(LockerItem li, BaseItem item, bool deleteFromDB)
        {
            _cashItems.RemoveItem(li, item);
            if (item != null)
                RemoveItem(item);

            if (deleteFromDB && li.SavedToDatabase) _cashItems.DeletedCashItems.Add(li.CashId);
        }

        public LockerItem GetLockerItemByCashID(long cashId)
        {
            return _cashItems.GetLockerItemFromCashID(cashId);
        }

        public BaseItem GetItemByCashID(long cashId, byte inventory)
        {
            BaseItem item = Items[inventory - 1].FirstOrDefault(x => x != null && x.CashId == cashId);
            if (item == null) item = Equips[0].FirstOrDefault(x => x != null && x.CashId == cashId);
            if (item == null) item = Equips[1].FirstOrDefault(x => x != null && x.CashId == cashId);
            return item;
        }

        public virtual void AddItem(byte inventory, short slot, BaseItem item, bool isLoading)
        {
            if (slot == 0)
            {
                // Would bug the client, so ignore
                Trace.WriteLine($"Ignoring item {item.ItemID} because its in the wrong slot (0)");
                return;
            }

            int itemid = item.ItemID;

            if (Constants.getInventory(itemid) != inventory)
            {
                Trace.WriteLine($"Ignoring item {item.ItemID} because its in the wrong inventory ({inventory} vs {Constants.getInventory(itemid)})");
                return;
            }

            item.InventorySlot = slot;

            short amount;
            if (!ItemAmounts.TryGetValue(itemid, out amount))
            {
                amount = 0;
            }
            amount += item.Amount;
            ItemAmounts[itemid] = amount;

            if (slot < 0)
            {
                if (item is EquipItem equipItem)
                {
                    slot = Math.Abs(slot);
                    if (slot > 100)
                    {
                        Equips[1][(byte)(slot - 100)] = equipItem;
                    }
                    else
                    {
                        Equips[0][(byte)slot] = equipItem;
                    }
                }
                else throw new Exception("Tried to AddItem on an equip slot but its not an equip! " + item);
            }
            else
            {
                Items[inventory - 1][slot] = item;
            }
        }

        public virtual void RemoveItem(BaseItem item)
        {
            var inventory = Constants.getInventory(item.ItemID);
            var slot = item.InventorySlot;
            int itemid = item.ItemID;

            if (slot == 0)
            {
                // Would bug the client, so ignore
                Trace.WriteLine($"Ignoring item {itemid} because its in the wrong slot (0)");
                return;
            }

            if (ItemAmounts.TryGetValue(itemid, out var amount))
            {
                if (amount - item.Amount <= 0) ItemAmounts.Remove(itemid);
                else ItemAmounts[itemid] -= item.Amount;
            }

            if (slot < 0)
            {
                if (item is EquipItem)
                {
                    slot = Math.Abs(slot);
                    if (slot > 100)
                    {
                        Equips[1][(byte)(slot - 100)] = null;
                    }
                    else
                    {
                        Equips[0][(byte)slot] = null;
                    }
                }
                else throw new Exception("Tried to RemoveItem on an equip slot but its not an equip! " + item);
            }
            else
            {
                Items[inventory - 1][slot] = null;
            }
        }

        public BaseItem GetItem(byte inventory, short slot)
        {
            inventory -= 1;
            if (inventory < 0 || inventory > 4)
            {
                return null;
            }

            BaseItem itm;
            if (slot < 0)
            {
                slot = Math.Abs(slot);
                // Equip.
                if (slot > 100)
                {
                    itm = Equips[1][(short)(slot - 100)];
                }
                else
                {
                    itm = Equips[0][slot];
                }
            }
            else
            {
                itm = Items[inventory][slot];
            }
            return itm;
        }

        public bool AddRockLocation(int map)
        {
            for (int i = 0; i < 5; i++)
            {
                if (TeleportRockLocations[i] == Constants.InvalidMap)
                {
                    TeleportRockLocations[i] = map;
                    return true;
                }
            }
            return false;
        }

        public bool RemoveRockLocation(int map)
        {
            for (int i = 0; i < 5; i++)
            {
                if (TeleportRockLocations[i] == map)
                {
                    TeleportRockLocations[i] = Constants.InvalidMap;
                    return true;
                }
            }
            return false;
        }

        public bool HasRockLocation(int map)
        {
            for (int i = 0; i < 5; i++)
            {
                if (TeleportRockLocations[i] == map)
                {
                    return true;
                }
            }
            return false;
        }

        public void GeneratePlayerPacket(Packet packet)
        {
            var shown = new List<byte>();
            
            foreach (var item in Equips[1])
            {
                if (item == null) continue;

                byte slotuse = (byte)Math.Abs(item.InventorySlot);
                if (slotuse > 100) slotuse -= 100;
                if (slotuse == (byte)Constants.EquipSlots.Slots.Weapon) continue;


                packet.WriteByte(slotuse);
                packet.WriteInt(item.ItemID);
                shown.Add(slotuse);
            }

            foreach (var item in Equips[0])
            {
                if (item == null) continue;
                var slotuse = (byte)Math.Abs(item.InventorySlot);

                if (shown.Contains(slotuse)) continue;

                packet.WriteByte(slotuse);
                packet.WriteInt(item.ItemID);
            }
            
        }

        public void GenerateInventoryPacket(Packet packet, CharacterDataFlag flags = CharacterDataFlag.All)
        {
            if (flags.HasFlag(CharacterDataFlag.Money))
            {
                packet.WriteInt(Mesos);
            }

            if (flags.HasFlag(CharacterDataFlag.Equips))
            {
                foreach (var item in Equips[0])
                {
                    if (item == null) continue;
                    BasePacketHelper.AddItemData(packet, item, item.InventorySlot, false);
                }
                packet.WriteByte(0);


                foreach (var item in Equips[1])
                {
                    if (item == null) continue;
                    BasePacketHelper.AddItemData(packet, item, item.InventorySlot, false);
                }
                packet.WriteByte(0);
            }


            for (var i = 0; i < 5; i++)
            {
                if (flags.HasFlag((CharacterDataFlag)((short)CharacterDataFlag.Equips << i)) == false) continue;

                packet.WriteByte(MaxSlots[i]);
                foreach (var item in Items[i])
                {
                    if (item != null && item.InventorySlot > 0)
                    {
                        BasePacketHelper.AddItemData(packet, item, item.InventorySlot, false);
                    }
                }
                packet.WriteByte(0);
            }
        }

        /// <summary>
        /// Set the MaxSlots for <param name="inventory"/> to <param name="slots" />.
        /// If the Items array is already initialized, it will either expand the array,
        /// or, when <param name="slots" /> is less, will remove items and shrink it.
        /// </summary>
        /// <param name="inventory">Inventory ID, 1-5</param>
        /// <param name="slots">Amount of slots</param>
        public virtual void SetInventorySlots(byte inventory, byte slots, bool sendPacket = true)
        {
            if (inventory < 1 || inventory > 5) throw new ArgumentException("Inventory out of range", nameof(inventory));

            inventory -= 1;
            if (slots < 24) slots = 24;
            if (slots > 100) slots = 100;


            var invArraySlots = slots + 1;
            if (Items[inventory] == null) Items[inventory] = new BaseItem[invArraySlots];
            else Array.Resize(ref Items[inventory], invArraySlots);

            MaxSlots[inventory] = slots;
        }

        public void AddRockPacket(Packet pw)
        {
            for (int i = 0; i < 5; i++)
            {
                pw.WriteInt(TeleportRockLocations[i]);
            }
        }

        public IEnumerable<PetItem> GetAlivePets()
        {
            return Items[4].Where(x => x != null && Constants.isPet(x.ItemID)).Select(x => x as PetItem);
        }
    }
}
