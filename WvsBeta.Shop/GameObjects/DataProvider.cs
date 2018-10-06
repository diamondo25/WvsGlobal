using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using reNX.NXProperties;

namespace WvsBeta.Shop
{
    public class DataProvider : BaseDataProvider
    {
        public static Dictionary<int, CommodityInfo> Commodity { get; } = new Dictionary<int, CommodityInfo>();
        public static Dictionary<int, int[]> Packages { get; } = new Dictionary<int, int[]>();

        public static void Load()
        {
            StartInit();
            LoadBase();

            Reload();

            FinishInit();
        }

        public static void Reload()
        {
            bool unload = false;
            if (pFile == null)
            {
                StartInit();
                unload = true;
            }

            Commodity.Clear();

            foreach (var node in pFile.BaseNode["Etc"]["Commodity.img"])
            {
                var snId = node["SN"].ValueInt32();
                var itemId = node["ItemId"].ValueInt32();


                var ci = Commodity[snId] = new CommodityInfo
                {
                    Count = node["Count"].ValueInt16(),
                    Gender = (CommodityGenders)node["Gender"].ValueByte(),
                    ItemID = itemId,
                    Period = node["Period"].ValueInt16(),
                    OnSale = node["OnSale"].ValueBool(),
                    Price = node["Price"].ValueInt16(),
                    SerialNumber = snId
                };

                if (!Items.ContainsKey(itemId) &&
                    !Equips.ContainsKey(itemId) &&
                    !Pets.ContainsKey(itemId))
                {
                    Program.MainForm.LogAppend("Ignoring commodity SN {0} as it contains unknown itemid {1}", snId, itemId);

                    ci.OnSale = false;
                    ci.StockState = StockState.NotAvailable;
                }

                if (ci.Price == 18000 && ci.OnSale)
                {
                    Program.MainForm.LogAppend("Making SN {0} itemid {1} not OnSale because its price is 18k", ci.SerialNumber, ci.ItemID);
                    ci.OnSale = false;
                    ci.StockState = StockState.NotAvailable;
                }
            }

            Program.MainForm.LogAppend("Loaded {0} commodity items!", Commodity.Count);

            Packages.Clear();


            foreach (var node in pFile.BaseNode["Etc"]["CashPackage.img"])
            {
                var sn = int.Parse(node.Name);
                var contents = node["SN"].Select(x => x.ValueInt32()).ToArray();
                var error = false;
                foreach (var commoditySN in contents)
                {
                    if (Commodity.ContainsKey(commoditySN) == false)
                    {
                        error = true;
                        Program.MainForm.LogAppend("Ignoring Package {0} as it contains invalid commodity id {1}", sn, commoditySN);
                        break;
                    }
                }
                if (!error)
                {
                    Packages[sn] = contents;
                }
            }


            Program.MainForm.LogAppend("Loaded {0} cash packages!", Packages.Count);

            if (unload)
            {
                FinishInit();
            }

        }


    }
}
