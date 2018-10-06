using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using MySql.Data.MySqlClient;
using WvsBeta.Common;
using WvsBeta.Database;
using WvsBeta.Game;

namespace WvsBeta.SharedDataProvider
{
    /// <summary>
    /// This class will handle the 'itemlocker' stuff. The itemlocker is a table used for
    /// managing the cash items in someones inventory (aka Storage) and locker (aka cashshop inventory).
    /// </summary>
    public class CharacterCashItems
    {
        public List<LockerItem> Items { get; private set; }
        public List<EquipItem> Equips { get; private set; }
        public List<BundleItem> Bundles { get; private set; }
        public List<PetItem> Pets { get; private set; }
        public List<long> DeletedCashItems { get; } = new List<long>();
        // TODO: Pets

        public static MySQL_Connection Connection { get; set; }
        public static byte WorldID { get; set; }

        public int UserID { get; set; }
        public int CharacterID { get; set; }

        public CharacterCashItems(int userId, int characterId)
        {
            UserID = userId;
            CharacterID = characterId;
        }

        // TODO: Move this to CenterServer
        public void Load()
        {
            Console.WriteLine("Loading starting. {0:O}", DateTime.Now);
            Items = new List<LockerItem>(GetLocker(UserID, CharacterID));
            Equips = new List<EquipItem>(GetCashEquips(UserID, CharacterID));
            Bundles = new List<BundleItem>(GetCashBundles(UserID, CharacterID));

            Pets = new List<PetItem>(GetPets(UserID, CharacterID));
            var allCashIds = Equips.Select(x => x.CashId).Union(Bundles.Select(x => x.CashId)).Union(Pets.Select(x => x.CashId));

            Console.WriteLine("Loading Done. {0:O} cashid match {1}", DateTime.Now, allCashIds.Count() == Items.Count);
        }

        public static void SaveMultiple(params CharacterCashItems[] stores)
        {
            var allDeletedItems = stores.SelectMany(x => x.DeletedCashItems).ToArray();

            if (allDeletedItems.Length > 0)
            {
                Console.WriteLine($"Deleting all items {string.Join(",", allDeletedItems)}");
                // Drop all deleted items
                Connection.RunTransaction(x =>
                {
                    x.CommandText = $"DELETE FROM cashitem_eqp WHERE cashid IN ({string.Join(",", allDeletedItems)});";
                    x.ExecuteNonQuery();

                    x.CommandText = $"DELETE FROM cashitem_bundle WHERE cashid IN ({string.Join(",", allDeletedItems)});";
                    x.ExecuteNonQuery();

                    x.CommandText = $"DELETE FROM cashitem_pet WHERE cashid IN ({string.Join(",", allDeletedItems)});";
                    x.ExecuteNonQuery();

                    x.CommandText = $"DELETE FROM itemlocker WHERE cashid IN ({string.Join(",", allDeletedItems)});";
                    x.ExecuteNonQuery();
                });
            }

            var allItems = stores.SelectMany(x => x.Items).ToList();
            if (allItems.Count > 0)
            {
                var cashidUseridMapping = allItems.Select(x => (x.CashId, x.UserId)).ToDictionary(x => x.Item1, x => x.Item2);
                var baseItemDict = new Dictionary<long, BaseItem>();

                var allEquips = stores.SelectMany(x => x.Equips).ToList();
                var allBundles = stores.SelectMany(x => x.Bundles).ToList();
                var allPets = stores.SelectMany(x => x.Pets).ToList();

                allEquips.ForEach(x => baseItemDict.Add(x.CashId, x));
                allBundles.ForEach(x => baseItemDict.Add(x.CashId, x));
                allPets.ForEach(x => baseItemDict.Add(x.CashId, x));

                // Save equips

                bool first = true;
                if (allEquips.Count > 0)
                {
                    Connection.RunTransaction(x =>
                    {
                        // Updating
                        first = true;
                        var sb = new StringBuilder();
                        foreach (var equip in allEquips.Where(y => y.AlreadyInDatabase == true))
                        {
                            var userId = cashidUseridMapping[equip.CashId];
                            sb.Append("UPDATE cashitem_eqp SET ");
                            sb.Append(equip.GetFullUpdateColumns());
                            sb.AppendLine($" WHERE cashid = {equip.CashId} AND userid = {userId};");
                            Console.WriteLine("Updating equip {0} cashid {1} userid {2}", equip.ItemID, equip.CashId, userId);
                            first = false;
                        }

                        if (first == false)
                        {
                            x.CommandText = sb.ToString();
                            x.ExecuteNonQuery();
                        }


                        // Insertion
                        first = true;
                        sb.Clear();
                        sb.AppendLine("INSERT INTO cashitem_eqp VALUES ");
                        foreach (var equip in allEquips.Where(y => y.AlreadyInDatabase == false))
                        {
                            if (first == false) sb.Append(',');
                            var userId = cashidUseridMapping[equip.CashId];
                            sb.AppendLine($"({userId}, {equip.GetFullSaveColumns()})");
                            Console.WriteLine("Inserting equip {0} cashid {1} userid {2}", equip.ItemID, equip.CashId, userId);
                            first = false;

                            equip.AlreadyInDatabase = true;
                        }
                        sb.Append(';');

                        if (first == false)
                        {
                            x.CommandText = sb.ToString();
                            x.ExecuteNonQuery();
                        }
                    });
                }

                // Save bundles

                if (allBundles.Count > 0)
                {
                    Connection.RunTransaction(x =>
                    {
                        // Updating
                        first = true;
                        var sb = new StringBuilder();
                        foreach (var bundle in allBundles.Where(y => y.AlreadyInDatabase == true))
                        {
                            var userId = cashidUseridMapping[bundle.CashId];
                            sb.Append("UPDATE cashitem_bundle SET ");
                            sb.Append(bundle.GetFullUpdateColumns());
                            sb.AppendLine($" WHERE cashid = {bundle.CashId} AND userid = {userId};");
                            Console.WriteLine("Updating bundle {0} cashid {1} userid {2}", bundle.ItemID, bundle.CashId, userId);
                            first = false;
                        }

                        if (first == false)
                        {
                            x.CommandText = sb.ToString();
                            x.ExecuteNonQuery();
                        }

                        // Insertion
                        first = true;
                        sb.Clear();
                        sb.AppendLine("INSERT INTO cashitem_bundle VALUES ");
                        foreach (var bundle in allBundles.Where(y => y.AlreadyInDatabase == false))
                        {
                            if (first == false) sb.Append(',');
                            var userId = cashidUseridMapping[bundle.CashId];
                            sb.AppendLine($"({userId}, {bundle.GetFullSaveColumns()})");
                            Console.WriteLine(
                                "Inserting bundle {0} cashid {1} userid {2}",
                                bundle.ItemID,
                                bundle.CashId,
                                userId
                            );

                            first = false;

                            bundle.AlreadyInDatabase = true;
                        }
                        sb.Append(';');

                        if (first == false)
                        {
                            x.CommandText = sb.ToString();
                            x.ExecuteNonQuery();
                        }
                    });
                }

                // Update pets

                if (allPets.Count > 0)
                {
                    Connection.RunTransaction(x =>
                    {
                        // Updating
                        first = true;
                        var sb = new StringBuilder();
                        foreach (var pet in allPets.Where(y => y.AlreadyInDatabase == true))
                        {
                            var userId = cashidUseridMapping[pet.CashId];
                            sb.Append("UPDATE cashitem_pet SET ");
                            sb.Append(pet.GetFullUpdateColumns());
                            sb.AppendLine($" WHERE cashid = {pet.CashId} AND userid = {userId};");
                            Console.WriteLine("Updating pet {0} cashid {1} userid {2}", pet.ItemID, pet.CashId, userId);
                            first = false;
                        }

                        if (first == false)
                        {
                            x.CommandText = sb.ToString();
                            x.ExecuteNonQuery();
                        }

                        // Insertion
                        first = true;
                        sb.Clear();
                        sb.AppendLine("INSERT INTO cashitem_pet VALUES ");
                        foreach (var pet in allPets.Where(y => y.AlreadyInDatabase == false))
                        {
                            if (first == false) sb.Append(',');
                            var userId = cashidUseridMapping[pet.CashId];
                            sb.AppendLine($"({userId}, {pet.GetFullSaveColumns()})");
                            Console.WriteLine(
                                "Inserting pet {0} cashid {1} userid {2}",
                                pet.ItemID,
                                pet.CashId,
                                userId
                            );

                            first = false;

                            pet.AlreadyInDatabase = true;
                        }
                        sb.Append(';');

                        if (first == false)
                        {
                            x.CommandText = sb.ToString();
                            x.ExecuteNonQuery();
                        }
                    });
                }


                // Save itemlocker

                Connection.RunTransaction(x =>
                {
                    // Updating
                    first = true;
                    var sb = new StringBuilder();

                    foreach (var item in allItems.Where(y => y.SavedToDatabase == true))
                    {
                        if (baseItemDict.TryGetValue(item.CashId, out var baseItem))
                        {
                            sb.Append("UPDATE itemlocker SET ");
                            sb.Append("slot = " + baseItem.InventorySlot + ",");
                            sb.Append(item.GetFullUpdateColumns());
                            sb.AppendLine($" WHERE cashid = {item.CashId} AND userid = {item.UserId};");
                            Console.WriteLine("Updating itemlocker {0} cashid {1} userid {2}", item.ItemId, item.CashId, item.UserId);

                            first = false;
                        }
                        else
                        {
                            Console.WriteLine("Found itemlocker item that doesnt have an actual item?? cashid {0}", item.CashId);
                        }
                    }

                    if (first == false)
                    {
                        x.CommandText = sb.ToString();
                        x.ExecuteNonQuery();
                    }

                    // Insertion
                    first = true;
                    sb.Clear();
                    sb.AppendLine("INSERT INTO itemlocker VALUES ");
                    foreach (var item in allItems.Where(y => y.SavedToDatabase == false))
                    {
                        if (first == false) sb.Append(',');

                        if (baseItemDict.TryGetValue(item.CashId, out var baseItem))
                        {
                            sb.AppendLine($"({item.CashId}, {baseItem.InventorySlot}, {item.UserId}, {item.CharacterId}, {item.ItemId}, {item.CommodityId}, {item.Amount}, '{MySqlHelper.EscapeString(item.BuyCharacterName)}', {item.Expiration}, {item.GiftUnread}, {WorldID})");
                            Console.WriteLine("Inserting itemlocker {0} cashid {1} userid {2}", item.ItemId, item.CashId, item.UserId);

                            first = false;
                            item.SavedToDatabase = true;
                        }
                        else
                        {
                            Console.WriteLine("Found itemlocker item that doesnt have an actual item?? cashid {0}", item.CashId);
                        }
                    }
                    sb.Append(';');

                    if (first == false)
                    {
                        x.CommandText = sb.ToString();
                        x.ExecuteNonQuery();
                    }
                });

            }
        }
        
        private static IEnumerable<LockerItem> GetLocker(int userId, int characterId)
        {
            using (var data = Connection.RunQuery(
                "SELECT * FROM itemlocker WHERE userid = @userid AND characterid = @charid AND worldid = @worldid ORDER BY slot ASC LIMIT 400",
                "@userid", userId,
                "@worldid", WorldID,
                "@charid", characterId
            ) as MySqlDataReader)
            {
                while (data.Read())
                {
                    var lockerItem = new LockerItem(data);
                    lockerItem.SavedToDatabase = true;
                    Console.WriteLine("Loading item {0} cashid {1}", lockerItem.ItemId, lockerItem.CashId);
                    yield return lockerItem;
                }
            }
        }


        private static IEnumerable<EquipItem> GetCashEquips(int userId, int characterId)
        {
            using (var data = Connection.RunQuery(
                @"SELECT ci.*, l.slot FROM cashitem_eqp ci JOIN itemlocker l ON l.cashid = ci.cashid AND l.userid = @userid AND l.characterid = @charid AND l.worldid = @worldid LIMIT 400",
                "@userid", userId,
                "@worldid", WorldID,
                "@charid", characterId
            ) as MySqlDataReader)
            {
                while (data.Read())
                {
                    var equip = BaseItem.CreateFromItemID(data.GetInt32("itemid"));
                    equip.Load(data);
                    equip.InventorySlot = data.GetInt16("slot");
                    Console.WriteLine("Loading equip {0} cashid {1}", equip.ItemID, equip.CashId);
                    yield return equip as EquipItem;
                }
            }
        }

        private static IEnumerable<BundleItem> GetCashBundles(int userId, int characterId)
        {
            using (var data = Connection.RunQuery(
                @"SELECT ci.*, l.slot FROM cashitem_bundle ci JOIN itemlocker l ON l.cashid = ci.cashid AND l.userid = @userid AND l.characterid = @charid AND l.worldid = @worldid LIMIT 400",
                "@userid", userId,
                "@worldid", WorldID,
                "@charid", characterId
            ) as MySqlDataReader)
            {
                while (data.Read())
                {
                    var bundleItem = BaseItem.CreateFromItemID(data.GetInt32("itemid"));
                    bundleItem.Load(data);
                    bundleItem.InventorySlot = data.GetInt16("slot");
                    Console.WriteLine("Loading bundle {0} cashid {1}", bundleItem.ItemID, bundleItem.CashId);
                    yield return bundleItem as BundleItem;
                }
            }
        }

        private static IEnumerable<PetItem> GetPets(int userId, int characterId)
        {
            using (var data = Connection.RunQuery(
                @"SELECT ci.*, l.slot FROM cashitem_pet ci JOIN itemlocker l ON l.cashid = ci.cashid AND l.userid = @userid AND l.characterid = @charid AND l.worldid = @worldid LIMIT 400",
                "@userid", userId,
                "@worldid", WorldID,
                "@charid", characterId
            ) as MySqlDataReader)
            {
                while (data.Read())
                {
                    var bundleItem = BaseItem.CreateFromItemID(data.GetInt32("itemid"));
                    bundleItem.Load(data);
                    bundleItem.InventorySlot = data.GetInt16("slot");
                    Console.WriteLine("Loading pet {0} cashid {1}", bundleItem.ItemID, bundleItem.CashId);
                    yield return bundleItem as PetItem;
                }
            }
        }


        public LockerItem GetLockerItemFromCashID(long cashId) => Items.FirstOrDefault(x => x.CashId == cashId);


        public BaseItem GetItemFromCashID(long cashId, int itemid = 0)
        {
            if (itemid == 0)
            {
                var possibleItem = GetLockerItemFromCashID(cashId);
                if (possibleItem == null) return null;
                itemid = possibleItem.ItemId;
            }

            if (Constants.isEquip(itemid))
                return Equips.FirstOrDefault(x => x.CashId == cashId);
            else if (Constants.isPet(itemid))
                return Pets.FirstOrDefault(x => x.CashId == cashId);
            else
                return Bundles.FirstOrDefault(x => x.CashId == cashId);
        }


        public void AddItem(LockerItem lockerItem, BaseItem baseItem)
        {
            Items.Add(lockerItem);

            if (baseItem is EquipItem ei)
                Equips.Add(ei);
            else if (baseItem is BundleItem bi)
                Bundles.Add(bi);
            else if (baseItem is PetItem pi)
                Pets.Add(pi);
        }

        public void RemoveItem(LockerItem lockerItem, BaseItem baseItem)
        {
            Items.Remove(lockerItem);
            
            if (baseItem is EquipItem ei)
                Equips.Remove(ei);
            else if (baseItem is BundleItem bi)
                Bundles.Remove(bi);
            else if (baseItem is PetItem pi)
                Pets.Remove(pi);
        }

        private long lastCheck = 0;
        public void GetExpiredItems(long time, Action<List<LockerItem>> callback)
        {
            if (time - lastCheck < 45000) return;
            lastCheck = time;

            if (Items.Count == 0) return;

            callback(Items.Where(x => x.Expiration < time).ToList());
        }

    }
}
