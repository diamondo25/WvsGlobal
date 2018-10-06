using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using reNX;
using reNX.NXProperties;
using WvsBeta.Common;
using WvsBeta.Game;

namespace WvsBeta.DataExporter
{
    class Program
    {
        class DummyMainForm : IMainForm
        {

            public void LogAppend(string pFormat, params object[] pParams)
            {
                Console.WriteLine("LogAppend(): {0}", string.Format(pFormat, pParams));
            }

            public void LogDebug(string pFormat, params object[] pParams)
            {
                Console.WriteLine("LogDebug(): {0}", string.Format(pFormat, pParams));
            }

            public void LogToFile(string what)
            {
                Console.WriteLine("LogToFile(): {0}", what);
            }

            public void ChangeLoad(bool up)
            {
                Console.WriteLine("ChangeLoad(): {0}", up);
            }

            public void Shutdown()
            {
                throw new NotImplementedException();
            }
        }

        static void ExportDrops()
        {
            using (var fs = File.Open("drops.tsv", FileMode.Create))
            using (var sw = new StreamWriter(fs))
            {
                sw.WriteLine("Dropper ID\tIs Mesos\tItem ID or amount of mesos\tMin\tMax\tChance\tExpire date\tPeriod\tPremium drop");
                foreach (var dropperAndData in Game.DataProvider.Drops)
                {
                    foreach (var drop in dropperAndData.Value)
                    {
                        bool isMesos = drop.Mesos > 0;
                        sw.WriteLine(
                            $"{dropperAndData.Key}\t{isMesos}\t{(isMesos ? drop.Mesos : drop.ItemID)}\t" +
                            $"{drop.Min}\t{drop.Max}\t{drop.Chance}\t" +
                            $"{drop.DateExpire}\t{drop.Period}\t{drop.Premium}");
                    }
                }

            }
        }

        static void Main(string[] args)
        {
            Game.Program.MainForm = new DummyMainForm();
            Game.DataProvider.Load();


            File.WriteAllText("drops.json", JsonConvert.SerializeObject(Game.DataProvider.Drops, Formatting.Indented));
            File.WriteAllText("items.json", JsonConvert.SerializeObject(Game.DataProvider.Items, Formatting.Indented));
            File.WriteAllText("equips.json", JsonConvert.SerializeObject(Game.DataProvider.Equips, Formatting.Indented));
            File.WriteAllText("mobs.json", JsonConvert.SerializeObject(Game.DataProvider.Mobs, Formatting.Indented));

            ExportDrops();

            // Cleanup footholds
            Game.DataProvider.Maps.ForEach(x =>
            {
                x.Value.SetFootholds(new List<Foothold>());
            });

            File.WriteAllText("maps.json", JsonConvert.SerializeObject(Game.DataProvider.Maps, Formatting.Indented, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            }));




            var categoryItemCount = new Dictionary<ushort, int>();
            var itemCategoryMapping = new Dictionary<ushort, int>();
            Action<byte, byte, Constants.Items.Types.ItemTypes> addItemTypeMap = (cat, catSub, slot) =>
            {
                itemCategoryMapping[(ushort)slot] = (10000000 * cat) + (100000 * catSub);
                categoryItemCount[(ushort)slot] = 0;
            };

            addItemTypeMap(2, 0, Constants.Items.Types.ItemTypes.ArmorHelm);
            addItemTypeMap(2, 1, Constants.Items.Types.ItemTypes.AccessoryFace);
            addItemTypeMap(2, 2, Constants.Items.Types.ItemTypes.AccessoryEye);
            addItemTypeMap(2, 3, Constants.Items.Types.ItemTypes.ArmorOverall);
            addItemTypeMap(2, 4, Constants.Items.Types.ItemTypes.ArmorTop);
            addItemTypeMap(2, 5, Constants.Items.Types.ItemTypes.ArmorBottom);
            addItemTypeMap(2, 6, Constants.Items.Types.ItemTypes.ArmorShoe);
            addItemTypeMap(2, 7, Constants.Items.Types.ItemTypes.ArmorGlove);

            addItemTypeMap(2, 8, Constants.Items.Types.ItemTypes.Weapon1hSword);
            addItemTypeMap(2, 8, Constants.Items.Types.ItemTypes.Weapon1hAxe);
            addItemTypeMap(2, 8, Constants.Items.Types.ItemTypes.Weapon1hMace);
            addItemTypeMap(2, 8, Constants.Items.Types.ItemTypes.WeaponDagger);
            addItemTypeMap(2, 8, Constants.Items.Types.ItemTypes.WeaponWand);
            addItemTypeMap(2, 8, Constants.Items.Types.ItemTypes.WeaponStaff);
            addItemTypeMap(2, 8, Constants.Items.Types.ItemTypes.Weapon2hSword);
            addItemTypeMap(2, 8, Constants.Items.Types.ItemTypes.Weapon2hAxe);
            addItemTypeMap(2, 8, Constants.Items.Types.ItemTypes.Weapon2hMace);
            addItemTypeMap(2, 8, Constants.Items.Types.ItemTypes.WeaponSpear);
            addItemTypeMap(2, 8, Constants.Items.Types.ItemTypes.WeaponPolearm);
            addItemTypeMap(2, 8, Constants.Items.Types.ItemTypes.WeaponBow);
            addItemTypeMap(2, 8, Constants.Items.Types.ItemTypes.WeaponCrossbow);
            addItemTypeMap(2, 8, Constants.Items.Types.ItemTypes.WeaponClaw);
            addItemTypeMap(2, 8, Constants.Items.Types.ItemTypes.WeaponCash);
            addItemTypeMap(2, 8, Constants.Items.Types.ItemTypes.ArmorShield);

            addItemTypeMap(2, 9, Constants.Items.Types.ItemTypes.ArmorRing);
            addItemTypeMap(2, 9, Constants.Items.Types.ItemTypes.AccessoryEarring);

            // Premium
            addItemTypeMap(2, 10, Constants.Items.Types.ItemTypes.ArmorPendant);
            addItemTypeMap(2, 10, Constants.Items.Types.ItemTypes.WeaponSkillFX);

            // Cape
            addItemTypeMap(2, 11, Constants.Items.Types.ItemTypes.ArmorCape);



            // Scroll
            addItemTypeMap(3, 0, Constants.Items.Types.ItemTypes.ItemScroll);
            addItemTypeMap(3, 0, Constants.Items.Types.ItemTypes.ItemReturnScroll);
            addItemTypeMap(3, 0, Constants.Items.Types.ItemTypes.ItemAPSPReset);
            // Messenger
            addItemTypeMap(3, 1, Constants.Items.Types.ItemTypes.ItemMegaPhone);
            // Weather
            addItemTypeMap(3, 2, Constants.Items.Types.ItemTypes.ItemWeather);



            // Beauty Parlor
            addItemTypeMap(5, 0, Constants.Items.Types.ItemTypes.EtcCoupon);

            // Store
            addItemTypeMap(5, 1, Constants.Items.Types.ItemTypes.EtcStorePermit);

            // Game
            addItemTypeMap(5, 2, Constants.Items.Types.ItemTypes.EtcEXPCoupon);
            addItemTypeMap(5, 2, Constants.Items.Types.ItemTypes.EtcGachaponTicket);
            addItemTypeMap(5, 2, Constants.Items.Types.ItemTypes.EtcChocolate);
            addItemTypeMap(5, 2, Constants.Items.Types.ItemTypes.EtcSafetyCharm);
            addItemTypeMap(5, 2, Constants.Items.Types.ItemTypes.ItemKite);
            addItemTypeMap(5, 2, Constants.Items.Types.ItemTypes.ItemMesoSack);
            addItemTypeMap(5, 2, Constants.Items.Types.ItemTypes.ItemNote);
            addItemTypeMap(5, 2, Constants.Items.Types.ItemTypes.ItemJukebox);
            addItemTypeMap(5, 2, Constants.Items.Types.ItemTypes.ItemTeleportRock);

            // Facial Expression
            addItemTypeMap(5, 3, Constants.Items.Types.ItemTypes.EtcEmote);

            // Anniversary
            // nothing?

            // Pet
            addItemTypeMap(6, 0, Constants.Items.Types.ItemTypes.Pet);
            // Pet Equip
            addItemTypeMap(6, 1, Constants.Items.Types.ItemTypes.PetEquip);
            addItemTypeMap(6, 1, Constants.Items.Types.ItemTypes.PetSkills);
            // Pet Use
            addItemTypeMap(6, 2, Constants.Items.Types.ItemTypes.EtcWaterOfLife);
            addItemTypeMap(6, 2, Constants.Items.Types.ItemTypes.ItemPetTag);
            addItemTypeMap(6, 2, Constants.Items.Types.ItemTypes.ItemPetFood);

            // Package

            var nxFileKVP = BaseDataProvider.GetMergedDatafiles();
            var nxFile = nxFileKVP.Key;

            var cashItemData = new Dictionary<int, List<(byte count, byte gender, byte onSale, byte period, int price, byte priority)>>();

            foreach (var commodityNode in nxFile.BaseNode["Etc"]["Commodity.img"])
            {
                var itemId = commodityNode["ItemId"].ValueInt32();
                var count = commodityNode["Count"].ValueByte();
                var gender = commodityNode["Gender"].ValueUInt8();
                var onSale = commodityNode["OnSale"].ValueUInt8();
                var period = commodityNode["Period"].ValueUInt8();
                var price = commodityNode["Price"].ValueInt32();
                var priority = commodityNode["Priority"].ValueUInt8();

                if (onSale == 0) onSale = 1;

                if (!cashItemData.ContainsKey(itemId))
                    cashItemData[itemId] = new List<(byte count, byte gender, byte onSale, byte period, int price, byte priority)>();

                cashItemData[itemId].Add((count, gender, onSale, period, price, priority));
            }

            using (var fs = File.Open("Commodity.img", FileMode.Create, FileAccess.Write, FileShare.Read))
            using (var sr = new StreamWriter(fs))
            using (var fsT = File.Open("Commodity.tsv", FileMode.Create, FileAccess.Write, FileShare.Read))
            using (var srT = new StreamWriter(fsT))
            {

                sr.WriteLine("#Property");
                var i = 0;

                var allCashItems = Game.DataProvider.Equips.Where(x => x.Value.Cash).Select(x => x.Key)
                    .Union(Game.DataProvider.Items.Where(x => x.Value.Cash).Select(x => x.Key))
                    .Union(Game.DataProvider.Pets.Select(x => x.Key))
                    .OrderBy(x => x);

                foreach (var itemId in allCashItems)
                {
                    var slot = (ushort)Constants.getItemType(itemId);

                    if (slot == 3 || slot == 2) continue;

                    if (!itemCategoryMapping.ContainsKey(slot))
                    {
                        Console.WriteLine("Item {0} ({1}) is not inside mapping.", itemId, (Constants.Items.Types.ItemTypes)slot);
                        continue;
                    }

                    void writeLine(byte count, byte gender, byte onSale, byte period, int price, byte priority)
                    {
                        var sn = itemCategoryMapping[slot];
                        var currentItemIndex = categoryItemCount[slot];
                        categoryItemCount[slot] += 1;
                        sn += currentItemIndex;

                        sr.WriteLine(i + " = {");
                        sr.WriteLine("\tSN = " + sn);
                        sr.WriteLine("\tCount = " + count);
                        sr.WriteLine("\tGender = " + gender);
                        sr.WriteLine("\tItemId = " + itemId);
                        sr.WriteLine("\tOnSale = " + onSale);
                        sr.WriteLine("\tPeriod = " + period);
                        sr.WriteLine("\tPrice = " + price);
                        sr.WriteLine("\tPriority = " + priority);
                        sr.WriteLine("}");
                        sr.WriteLine("");


                        srT.WriteLine($"{sn}\t{count}\t{gender}\t{itemId}\t{onSale}\t{period}\t{price}\t{priority}");

                        i++;
                    }


                    if (cashItemData.TryGetValue(itemId, out var alreadyFound))
                    {
                        foreach (var valueTuple in alreadyFound)
                        {
                            writeLine(valueTuple.count, valueTuple.gender, valueTuple.onSale, valueTuple.period,
                                valueTuple.price, valueTuple.priority);
                        }
                    }
                    else
                    {
                        writeLine(1, 2, 0, 90, 18000, 0);
                    }

                }
                Console.WriteLine("Wrote " + i + " cashitems");
            }

            ExportItemNames(nxFile);
            ExportMobNames(nxFile);
            ExportNpcNames(nxFile);
            ExportAllStrings(nxFile);

            Console.WriteLine("Done.");
            Console.ReadLine();
        }

        private static void ExportItemNames(NXFile nxFile)
        {
            var sb = new StringBuilder();

            void printItemName(NXNode item)
            {
                var hasName = item.ContainsChild("name");
                byte level = 0;
                var itemid = int.Parse(item.Name.TrimStart('0'));
                if (Constants.isEquip(itemid))
                    if (DataProvider.Equips.TryGetValue(itemid, out var ed)) level = ed.RequiredLevel;

                sb.AppendFormat("{0}\t{1}\t{2}\r\n", itemid, hasName ? item["name"].ValueString() : "-- NO NAME --", level);

            }

            foreach (var cat in nxFile.BaseNode["String"]["Item.img"]["Eqp"])
            {
                foreach (var item in cat)
                {
                    printItemName(item);
                }
            }
            foreach (var cat in nxFile.BaseNode["String"]["Item.img"].Where(x => x.Name != "Eqp"))
            {
                foreach (var item in cat)
                {
                    printItemName(item);
                }
            }

            File.WriteAllText("ids-of-items.tsv", sb.ToString());
        }

        private static void ExportMobNames(NXFile nxFile)
        {
            var sb = new StringBuilder();

            foreach (var mob in nxFile.BaseNode["String"]["Mob.img"])
            {
                sb.AppendFormat("{0}\t{1}\r\n", mob.Name, mob["name"].ValueString());
            }

            File.WriteAllText("ids-of-mobs.tsv", sb.ToString());
        }

        private static void ExportNpcNames(NXFile nxFile)
        {
            var sb = new StringBuilder();

            foreach (var npc in nxFile.BaseNode["String"]["Npc.img"])
            {
                sb.AppendFormat("{0}\t{1}\t{2}\r\n", npc.Name, npc["name"].ValueString(), string.Join("\t", npc.Where(x => x.Name.StartsWith("n") && x.Name != "name").Select(x => x.ValueString())));
            }

            File.WriteAllText("ids-of-npcs.tsv", sb.ToString());
        }

        private static void ExportAllStrings(NXFile file)
        {
            var sb = new StringBuilder();
            foreach (var rootNode in file.BaseNode)
            {
                if (rootNode.Name == "Character" ||
                    rootNode.Name == "smap.img") continue;
                IterateNodes(rootNode, "Data", sb);
            }
            File.WriteAllText("allStrings.tsv", sb.ToString());
        }

        private static void IterateNodes(NXNode n, string path, StringBuilder sb)
        {
            if (n.Name == "tile" ||
                n.Name == "obj" ||
                n.Name == "bS" ||
                n.Name == "tS" ||
                n.Name == "hs" ||
                n.Name == "action" ||
                n.Name == "id" ||
                n.Name == "type" ||
                n.Name == "tn" ||
                n.Name == "pn" ||
                n.Name == "interact" ||
                n.Name == "speak" ||
                n.Name == "delay" ||
                n.Name == "elemAttr" ||
                n.Name == "link" ||
                n.Name == "foothold" ||
                n.Name == "a0" ||
                n.Name == "a1" ||
                n.Name == "ladderRope") return;

            if (path.Contains("life")) return;

            var curPath = path + '/' + n.Name;
            if (n is NXValuedNode<string>)
                sb.AppendLine(curPath + "\tstring\t" + n.ValueString());
            //else if (n is NXValuedNode<Int64>)
            //    sb.AppendLine(curPath + "\tint\t" + n.ValueInt64());
            //else if (n is NXValuedNode<Double>)
            //    sb.AppendLine(curPath + "\tdouble\t" + n.ValueDouble());
            else
            {
                foreach (var subNode in n)
                {
                    IterateNodes(subNode, curPath, sb);
                }
            }
        }
    }
}
