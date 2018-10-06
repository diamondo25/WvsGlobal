using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using WvsBeta.Database;
using WvsBeta.Game;

namespace WvsBeta.SharedDataProvider
{
    public static class SplitDBInventory
    {
        public enum InventoryType
        {
            Eqp,
            Bundle,
        }

        private static string GetInventoryTableName(InventoryType type, string baseTableName)
        {
            switch (type)
            {
                case InventoryType.Eqp: return baseTableName + "_eqp";
                case InventoryType.Bundle: return baseTableName + "_bundle";
                default: throw new Exception();
            }
        }

        public delegate void AddItemCallback(InventoryType type, byte inventory, short slot, BaseItem item);

        public static void Load(MySQL_Connection connection, string baseTableName, string whereStatement, AddItemCallback callback)
        {
            using (var data = connection.RunQuery($"SELECT * FROM {GetInventoryTableName(InventoryType.Eqp, baseTableName)} WHERE {whereStatement}") as MySqlDataReader)
            {
                while (data.Read())
                {
                    var item = BaseItem.CreateFromItemID(data.GetInt32("itemid"));
                    item.Load(data);
                    callback(InventoryType.Eqp, 1, data.GetInt16("slot"), item);
                }
            }

            using (var data = connection.RunQuery($"SELECT * FROM {GetInventoryTableName(InventoryType.Bundle, baseTableName)} WHERE {whereStatement}") as MySqlDataReader)
            {
                while (data.Read())
                {
                    var item = BaseItem.CreateFromItemID(data.GetInt32("itemid"));
                    item.Load(data);
                    callback(InventoryType.Bundle, (byte)data.GetInt16("inv"), data.GetInt16("slot"), item);
                }
            }
        }


        public delegate IEnumerable<BaseItem> StoredItemsCallback(InventoryType type, byte inventory);
        public static void Save(MySQL_Connection connection, string baseTableName, string columnsBeforeItemInfo, string whereStatement, StoredItemsCallback callback, MySQL_Connection.LogAction dbgCallback)
        {

            #region bundle


            connection.RunTransaction(comm =>
            {
                var tableName = GetInventoryTableName(InventoryType.Bundle, baseTableName);

                comm.CommandText = $"DELETE FROM {tableName} WHERE {whereStatement}";
                comm.ExecuteNonQuery();

                var itemQuery = new StringBuilder();

                bool firstrun = true;
                // Inventories
                for (byte inventory = 2; inventory <= 5; inventory++)
                {
                    var items = callback(InventoryType.Bundle, inventory);

                    foreach (var item in items)
                    {
                        if (!(item is BundleItem)) continue;

                        if (firstrun)
                        {
                            itemQuery.Append($"INSERT INTO {tableName} VALUES (");
                            firstrun = false;
                        }
                        else
                        {
                            itemQuery.Append(", (");
                        }

                        itemQuery.Append(columnsBeforeItemInfo);
                        itemQuery.Append(inventory + ", ");
                        itemQuery.Append(item.InventorySlot + ", ");
                        itemQuery.Append(item.GetFullSaveColumns());
                        itemQuery.AppendLine(")");
                        
                    }
                    
                }

                if (itemQuery.Length == 0) return;

                comm.CommandText = itemQuery.ToString();
                comm.ExecuteNonQuery();

            }, dbgCallback);

            #endregion

            #region eqp

            connection.RunTransaction(comm =>
            {
                var tableName = GetInventoryTableName(InventoryType.Eqp, baseTableName);
                comm.CommandText = $"DELETE FROM {tableName} WHERE {whereStatement}";
                comm.ExecuteNonQuery();

                var itemQuery = new StringBuilder();

                bool firstrun = true;

                var equips = callback(InventoryType.Eqp, 1);
                foreach (var item in equips)
                {
                    if (item == null) continue;

                    if (firstrun)
                    {
                        itemQuery.Append($"INSERT INTO {tableName} VALUES (");
                        firstrun = false;
                    }
                    else
                    {
                        itemQuery.Append(", (");
                    }


                    itemQuery.Append(columnsBeforeItemInfo);
                    itemQuery.Append(item.InventorySlot + ", ");
                    itemQuery.Append(item.GetFullSaveColumns());
                    itemQuery.AppendLine(")");
                }

                if (itemQuery.Length == 0) return;

                comm.CommandText = itemQuery.ToString();
                comm.ExecuteNonQuery();

            }, dbgCallback);

            #endregion
        }

    }
}
