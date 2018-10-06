using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using reNX;
using reNX.NXProperties;
using WvsBeta.Common;
using WvsBeta.Game;

public abstract class BaseDataProvider
{
    public static IDictionary<int, EquipData> Equips { get; private set; }
    public static IDictionary<int, ItemData> Items { get; private set; } 
    public static IDictionary<int, PetData> Pets { get; private set; }
    public static List<int> UntradeableDrops { get; } = new List<int>();
    public static List<int> QuestItems { get; } = new List<int>();

    protected static List<NXFile> pOverride = new List<NXFile>();
    protected static NXFile pFile;
    private static DateTime startTime;

    public static KeyValuePair<NXFile, List<NXFile>> GetMergedDatafiles()
    {
        var mainFile = new NXFile(Path.Combine(Environment.CurrentDirectory, "..", "DataSvr", "Data.nx"));
        var overrideFolder = Path.Combine(Environment.CurrentDirectory, "..", "DataSvr", "data");
        var otherFiles = new List<NXFile>();
        if (Directory.Exists(overrideFolder))
        {
            otherFiles.AddRange(Directory
                .GetFiles(overrideFolder)
                .Where(f => f.EndsWith(".nx"))
                .Select(nxPath => new NXFile(nxPath))
            );

            foreach (var nxFile in otherFiles)
            {
                Console.WriteLine("Importing " + nxFile.FilePath);
                mainFile.Import(nxFile);
            }
        }

        return new KeyValuePair<NXFile, List<NXFile>>(mainFile, otherFiles);
    }

    protected static void StartInit()
    {
        startTime = DateTime.Now;
        var x = GetMergedDatafiles();
        pFile = x.Key;
        pOverride.AddRange(x.Value);
    }

    protected static void FinishInit()
    {
        Trace.WriteLine("Finished loading all WZ data in " + (DateTime.Now - startTime).TotalMilliseconds + " ms");
        pOverride.ForEach(x => x.Dispose());
        pOverride.Clear();
        pOverride = null;
        pFile.Dispose();
        pFile = null;

        // do some cleanup
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        GC.WaitForPendingFinalizers();
    }

    public static void LoadBase()
    {
        Action[] actions = 
        {
            ReadEquips,
            ReadItems,
            ReadPets,
        };


#if DEBUG
        foreach (var act in actions)
        {
            act();
        }
#else
        Task.WaitAll(actions.Select(x => Task.Factory.StartNew(x)).ToArray());
#endif
        ReadItemNames();
    }

    protected static void IterateAll<T>(IEnumerable<T> elements, Action<T> func)
    {
#if DEBUG
        foreach (var nxNode in elements)
        {
            func(nxNode);
        }
#else
        Parallel.ForEach(elements, func);
#endif
    }

    /// <summary>
    /// Iterate over each element (Parallel in non-debug mode) and make a Dictionary as result
    /// </summary>
    /// <typeparam name="TIn">Input type of iterator</typeparam>
    /// <typeparam name="TOut">Iterations output type</typeparam>
    /// <typeparam name="TKey">Dictionary Key type</typeparam>
    /// <param name="elements">Elements to iterate over</param>
    /// <param name="func">Function to run per iteration</param>
    /// <param name="outToKey">Function to convert the output to a key</param>
    /// <returns>A regular Dictionary with the key/value pairs</returns>
    protected static Dictionary<TKey, TOut> IterateAllToDict<TIn, TOut, TKey>(
        IEnumerable<TIn> elements,
        Func<TIn, TOut> func,
        Func<TOut, TKey> outToKey) =>
        IterateAllToDict(elements, func, outToKey, x => x);

    protected static Dictionary<TKey, TVal> IterateAllToDict<TIn, TOut, TKey, TVal>(IEnumerable<TIn> elements, Func<TIn, TOut> func, Func<TOut, TKey> outToKey, Func<TOut, TVal> outToVal)
    {
#if DEBUG
        var dict = new Dictionary<TKey, TVal>();
        foreach (var nxNode in elements)
        {
            var ret = func(nxNode);
            dict[outToKey(ret)] = outToVal(ret);
        }
        return dict;
#else
        return elements.AsParallel().Select(x => func(x)).ToDictionary(outToKey, outToVal);
#endif
    }

    protected static void ReadEquips()
    {
        var equips =
            from category in pFile.BaseNode["Character"]
            where category.Name.EndsWith(".img") == false && category.Name != "Afterimage"
            from item in category
            select new { category.Name, item };

        Equips = IterateAllToDict(equips, p =>
        {
            EquipData eq = new EquipData();
            var infoBlock = p.item["info"];
            eq.ID = (int)Utils.ConvertNameToID(p.item.Name);

            foreach (var nxNode in infoBlock)
            {
                switch (nxNode.Name)
                {
                    case "islot":
                    case "vslot":
                    case "icon":
                    case "iconRaw":
                    case "afterImage":
                    case "sfx":
                    case "attack":
                    case "stand":
                    case "walk":
                    case "sample":
                    case "chatBalloon":
                    case "nameTag":
                        break;

                    // Nexon typos (= not used!)
                    case "incMMD": // Green Jester, would've been a really good buff!
                    case "regPOP": // Dark Lucida (Female)
                        break;

                    case "tuc": eq.Slots = nxNode.ValueByte(); break;
                    case "reqLevel": eq.RequiredLevel = nxNode.ValueByte(); break;
                    case "reqPOP": eq.RequiredFame = nxNode.ValueByte(); break;
                    case "reqDEX": eq.RequiredDexterity = nxNode.ValueUInt16(); break;
                    case "reqINT": eq.RequiredIntellect = nxNode.ValueUInt16(); break;
                    case "reqLUK": eq.RequiredLuck = nxNode.ValueUInt16(); break;
                    case "reqSTR": eq.RequiredStrength = nxNode.ValueUInt16(); break;
                    case "reqJob":
                        {
                            var job = nxNode.ValueInt64();
                            // Sake Bottle and Tuna patch; ingame all jobs are required and the flag is -1..

                            if (job == -1) eq.RequiredJob = 0xFFFF;
                            else eq.RequiredJob = (ushort)job;
                            break;
                        }
                    case "price": eq.Price = nxNode.ValueInt32(); break;
                    case "incSTR": eq.Strength = nxNode.ValueInt16(); break;
                    case "incDEX": eq.Dexterity = nxNode.ValueInt16(); break;
                    case "incLUK": eq.Luck = nxNode.ValueInt16(); ; break;
                    case "incINT": eq.Intellect = nxNode.ValueInt16(); break;
                    case "incMDD": eq.MagicDefense = nxNode.ValueByte(); break;
                    case "incPDD": eq.WeaponDefense = nxNode.ValueByte(); break;
                    case "incPAD": eq.WeaponAttack = nxNode.ValueByte(); break;
                    case "incMAD": eq.MagicAttack = nxNode.ValueByte(); break;
                    case "incSpeed": eq.Speed = nxNode.ValueByte(); break;
                    case "incJump": eq.Jump = nxNode.ValueByte(); break;
                    case "incACC": eq.Accuracy = nxNode.ValueByte(); break;
                    case "incEVA": eq.Avoidance = nxNode.ValueByte(); break;
                    case "incMHP": eq.HP = nxNode.ValueInt16(); break;
                    case "incMMP": eq.MP = nxNode.ValueInt16(); break;
                    case "quest":
                        if (nxNode.ValueBool())
                        {
                            lock (UntradeableDrops)
                            {
                                lock(QuestItems)
                                {
                                    QuestItems.Add(eq.ID);
                                    if (!UntradeableDrops.Contains(eq.ID))
                                        UntradeableDrops.Add(eq.ID);
                                }
                            }

                        }
                        break;
                    case "only":
                        if (nxNode.ValueBool())
                        {
                            lock (UntradeableDrops)
                            {
                                if (!UntradeableDrops.Contains(eq.ID))
                                    UntradeableDrops.Add(eq.ID);
                            }
                        }
                        break;
                    case "cash": eq.Cash = nxNode.ValueBool(); break;
                    case "attackSpeed": eq.AttackSpeed = nxNode.ValueByte(); break;
                    case "knockback": eq.KnockbackRate = nxNode.ValueByte(); break;
                    case "timeLimited": eq.TimeLimited = nxNode.ValueBool(); break;
                    case "recovery": eq.RecoveryRate = nxNode.ValueFloat(); break;
                    default:
                        Console.WriteLine($"Unhandled node {nxNode.Name} for equip {eq.ID}");
                        break;
                }
            }
            return eq;
        }, x => x.ID, x => x);
    }

    protected static void ReadItems()
    {
        var items =
            from category in pFile.BaseNode["Item"]
            where category.Name != "Pet"
            from itemType in category
            from item in itemType
            select item;

        Items = IterateAllToDict(items, p =>
        {
            var iNode = p;
            ItemData item = new ItemData();
            int ID = (int)Utils.ConvertNameToID(iNode.Name);
            item.ID = ID;

            if (iNode.ContainsChild("info"))
            {
                var infoNode = iNode["info"];
                foreach (var node in infoNode)
                {
                    switch (node.Name)
                    {
                        case "path":
                        case "floatType":
                        case "unitPrice": // Pricing of recharging???
                        case "icon":
                        case "iconRaw":
                        case "iconReward": break;

                        case "type":
                            item.Type = node.ValueInt8();
                            break;
                        case "price":
                            item.Price = node.ValueInt32();
                            break;
                        case "timeLimited":
                            item.TimeLimited = node.ValueBool();
                            break;
                        case "cash":
                            item.Cash = node.ValueBool();
                            break;
                        case "slotMax":
                            item.MaxSlot = node.ValueUInt16();
                            break;
                        case "meso":
                            item.Mesos = node.ValueInt32();
                            break;
                        case "quest":
                            if (node.ValueBool())
                            {
                                lock (UntradeableDrops)
                                {
                                    lock (QuestItems)
                                    {
                                        item.IsQuest = true;
                                        UntradeableDrops.Add(item.ID);
                                        QuestItems.Add(item.ID);
                                    }
                                }
                            }
                            break;
                        case "success":
                            item.ScrollSuccessRate = node.ValueByte();
                            break;
                        case "cursed":
                            item.ScrollCurseRate = node.ValueByte();
                            break;
                        case "incSTR":
                            item.IncStr = node.ValueByte();
                            break;
                        case "incDEX":
                            item.IncDex = node.ValueByte();
                            break;
                        case "incLUK":
                            item.IncLuk = node.ValueByte();
                            break;
                        case "incINT":
                            item.IncInt = node.ValueByte();
                            break;
                        case "incMHP":
                            item.IncMHP = node.ValueByte();
                            break;
                        case "incMMP":
                            item.IncMMP = node.ValueByte();
                            break;
                        case "pad":
                        case "incPAD":
                            item.IncWAtk = node.ValueByte();
                            break;
                        case "incMAD":
                            item.IncMAtk = node.ValueByte();
                            break;
                        case "incPDD":
                            item.IncWDef = node.ValueByte();
                            break;
                        case "incMDD":
                            item.IncMDef = node.ValueByte();
                            break;
                        case "incACC":
                            item.IncAcc = node.ValueByte();
                            break;
                        case "incEVA":
                            item.IncAvo = node.ValueByte();
                            break;
                        case "incJump":
                            item.IncJump = node.ValueByte();
                            break;
                        case "incSpeed":
                            item.IncSpeed = node.ValueByte();
                            break;
                        case "rate":
                            item.Rate = node.ValueByte();
                            break;
                        case "only":
                            if (node.ValueBool())
                            {
                                lock (UntradeableDrops)
                                {
                                    UntradeableDrops.Add(item.ID);
                                }
                            }
                            break;
                        case "time":
                            item.RateTimes = new Dictionary<byte, List<KeyValuePair<byte, byte>>>();
                            foreach (var lNode in node)
                            {
                                string val = lNode.ValueString();
                                string day = val.Substring(0, 3);
                                byte hourStart = byte.Parse(val.Substring(4, 2));
                                byte hourEnd = byte.Parse(val.Substring(7, 2));
                                byte dayid = 0;

                                switch (day)
                                {
                                    case "MON": dayid = 0; break;
                                    case "TUE": dayid = 1; break;
                                    case "WED": dayid = 2; break;
                                    case "THU": dayid = 3; break;
                                    case "FRI": dayid = 4; break;
                                    case "SAT": dayid = 5; break;
                                    case "SUN": dayid = 6; break;
                                    case "HOL": dayid = ItemData.HOLIDAY_DAY; break;
                                }
                                if (!item.RateTimes.ContainsKey(dayid))
                                    item.RateTimes.Add(dayid, new List<KeyValuePair<byte, byte>>());

                                item.RateTimes[dayid].Add(new KeyValuePair<byte, byte>(hourStart, hourEnd));
                            }
                            break;
                        default:
                            Console.WriteLine($"Unhandled item info node {node.Name} for id {item.ID}");
                            break;

                    }
                }
            }
            else
            {
                item.Price = 0;
                item.Cash = false;
                item.MaxSlot = 1;
                item.Mesos = 0;
                item.IsQuest = false;

                item.ScrollSuccessRate = 0;
                item.ScrollCurseRate = 0;
                item.IncStr = 0;
                item.IncDex = 0;
                item.IncInt = 0;
                item.IncLuk = 0;
                item.IncMHP = 0;
                item.IncMMP = 0;
                item.IncWAtk = 0;
                item.IncMAtk = 0;
                item.IncWDef = 0;
                item.IncMDef = 0;
                item.IncAcc = 0;
                item.IncAvo = 0;
                item.IncJump = 0;
                item.IncSpeed = 0;
                item.Rate = 0;
            }
            if (iNode.ContainsChild("spec"))
            {
                var specNode = iNode["spec"];
                foreach (var node in specNode)
                {
                    switch (node.Name)
                    {
                        case "moveTo":
                            item.MoveTo = node.ValueInt32();
                            break;
                        case "hp":
                            item.HP = node.ValueInt16();
                            break;
                        case "mp":
                            item.MP = node.ValueInt16();
                            break;
                        case "hpR":
                            item.HPRate = node.ValueInt16();
                            break;
                        case "mpR":
                            item.MPRate = node.ValueInt16();
                            break;
                        case "speed":
                            item.Speed = node.ValueInt16();
                            break;
                        case "eva":
                            item.Avoidance = node.ValueInt16();
                            break;
                        case "acc":
                            item.Accuracy = node.ValueInt16();
                            break;
                        case "mad":
                            item.MagicAttack = node.ValueInt16();
                            break;
                        case "pad":
                            item.WeaponAttack = node.ValueInt16();
                            break;
                        case "pdd":
                            item.WeaponDefense = node.ValueInt16();
                            break;
                        case "thaw":
                            item.Thaw = node.ValueInt16();
                            break;
                        case "time":
                            item.BuffTime = node.ValueInt32();
                            break;

                        case "curse":
                        case "darkness":
                        case "poison":
                        case "seal":
                        case "weakness":
                            if (node.ValueInt64() != 0)
                            {
                                ItemData.CureFlags flag = 0;
                                switch (node.Name)
                                {
                                    case "curse":
                                        flag = ItemData.CureFlags.Curse;
                                        break;
                                    case "darkness":
                                        flag = ItemData.CureFlags.Darkness;
                                        break;
                                    case "poison":
                                        flag = ItemData.CureFlags.Poison;
                                        break;
                                    case "seal":
                                        flag = ItemData.CureFlags.Seal;
                                        break;
                                    case "weakness":
                                        flag = ItemData.CureFlags.Weakness;
                                        break;
                                }
                                item.Cures |= flag;
                            }
                            break;
                        default:
                            Console.WriteLine($"Unhandled item spec node {node.Name} for id {item.ID}");
                            break;
                    }
                }
            }
            else
            {
                //no spec, continue
                item.MoveTo = 0;
                item.HP = 0;
                item.MP = 0;
                item.HPRate = 0;
                item.MPRate = 0;
                item.Speed = 0;
                item.Avoidance = 0;
                item.Accuracy = 0;
                item.MagicAttack = 0;
                item.WeaponAttack = 0;
                item.BuffTime = 0;
            }


            if (iNode.ContainsChild("mob")) //summons
            {
                item.Summons = new List<ItemSummonInfo>();

                foreach (var sNode in iNode["mob"])
                {
                    item.Summons.Add(new ItemSummonInfo
                    {
                        MobID = sNode["id"].ValueInt32(),
                        Chance = sNode["prob"].ValueByte()
                    });
                }

            }
            return item;
        }, x => x.ID);

    }

    protected static void ReadPets()
    {
        Pets = IterateAllToDict(pFile.BaseNode["Item"]["Pet"], pNode =>
        {
            int ID = (int) Utils.ConvertNameToID(pNode.Name);
            var pd = new PetData
            {
                ID = ID,
                Reactions = new Dictionary<byte, PetReactionData>()
            };

            foreach (var mNode in pNode["interact"])
            {
                var prd = new PetReactionData
                {
                    ReactionID = byte.Parse(mNode.Name),
                    Inc = mNode["inc"].ValueByte(),
                    Prob = mNode["prob"].ValueByte(),
                    LevelMin = mNode["l0"].ValueByte(),
                    LevelMax = mNode["l1"].ValueByte()
                };
                pd.Reactions.Add(prd.ReactionID, prd);
            }

            foreach (var node in pNode["info"])
            {
                switch (node.Name)
                {
                    case "icon":
                    case "iconD":
                    case "iconRaw":
                    case "iconRawD":
                    case "cash":
                        break;

                    case "hungry":
                        pd.Hungry = node.ValueByte();
                        break;
                    case "life":
                        pd.Life = node.ValueByte();
                        break;

                    default:
                        Console.WriteLine($"Unhandled Pet node {node.Name} for id {ID}");
                        break;
                }
            }
            return pd;
        }, x => x.ID);

        foreach (var node in pFile.BaseNode["String"]["Item.img"]["Pet"])
        {
            var itemId = int.Parse(node.Name);
            if (!Pets.ContainsKey(itemId))
                Pets[itemId].Name = node["name"].ValueString();
        }
    }


    private static void ProcessNames(NXNode listNode, Action<int, string> handleName)
    {
        foreach (var item in listNode)
        {
            if (int.TryParse(item.Name, out var itemId))
            {
                if (item.ContainsChild("name"))
                {
                    handleName(itemId, item["name"].ValueString());
                }
                else
                {
                    Trace.WriteLine($"Item {itemId} does not contain 'name' node.");
                }
            }
            else
            {
                Trace.WriteLine($"Node {item.Name} does not have a valid itemid as name!?");
            }

        }
    }

    public static void ReadItemNames()
    {

        foreach (var node in pFile.BaseNode["String"]["Item.img"])
        {
            if (node.Name == "Eqp")
            {
                foreach (var cat in node)
                {
                    ProcessNames(cat, (i, s) =>
                    {
                        if (!Equips.ContainsKey(i))
                            Trace.WriteLine($"Found name {s} for equip {i}, but equip did not exist!");
                        else
                            Equips[i].Name = s;
                    });
                }
            }
            else if (node.Name == "Pet")
            {
                ProcessNames(node, (i, s) =>
                {
                    if (!Pets.ContainsKey(i))
                        Trace.WriteLine($"Found name {s} for pet {i}, but pet did not exist!");
                    else
                        Pets[i].Name = s;
                });
            }
            else
            {
                ProcessNames(node, (i, s) =>
                {
                    if (!Items.ContainsKey(i))
                        Trace.WriteLine($"Found name {s} for item {i}, but item did not exist!");
                    else
                        Items[i].Name = s;
                });
            }
        }
    }
}
