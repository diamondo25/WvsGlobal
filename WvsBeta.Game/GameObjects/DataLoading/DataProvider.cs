using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using reNX;
using reNX.NXProperties;
using WvsBeta.Common;
using WvsBeta.Game.GameObjects;
using WvsBeta.Game.GameObjects.DataLoading;

// if \(.*Node.ContainsChild\((.*)\)\)[\s\r\n]+\{[\s\r\n]+(.*)\r\n[\s\r\n]+\}
// case $1: $2 break;
namespace WvsBeta.Game
{
    public class DataProvider : BaseDataProvider
    {
        public static IDictionary<int, Map> Maps { get; private set; }
        public static IDictionary<int, NPCData> NPCs { get; private set; }
        public static IDictionary<int, MobData> Mobs { get; private set; }
        public static List<int> Jobs { get; private set; }
        public static IDictionary<int, SkillData> Skills { get; private set; }
        public static IDictionary<byte, Dictionary<byte, MobSkillLevelData>> MobSkills { get; private set; }
        public static IDictionary<string, DropData[]> Drops { get; private set; }
        public static IDictionary<int, Questdata> Quests { get; private set; }
        public static Dictionary<byte, List<QuizData>> QuizQuestions { get; } = new Dictionary<byte, List<QuizData>>();

        private static NXFile pDropFile;


        public static void Load()
        {
            StartInit();

            using (pDropFile = new NXFile(Path.Combine(Environment.CurrentDirectory, "..", "DataSvr", "DropData.nx"), NXReadSelection.None))
            {
                var funcs = new Action[] {
                    LoadBase,
                    ReadMobData,
                    ReadMapData,
                    ReadNpcs,
                    ReadSkills,
                    ReadDrops,
                    ReadQuiz
                };

#if DEBUG
                foreach (var func in funcs)
                {
                    func();
                }
#else
                Task.WaitAll(funcs.Select(Task.Run).ToArray());
#endif

                // Cleanup the drops from nonexistant items and droppers
                
                foreach (var kvp in Drops.ToList())
                {
                    if (kvp.Key.StartsWith("m"))
                    {
                        var mobId = int.Parse(kvp.Key.Substring(1));
                        if (!Mobs.ContainsKey(mobId))
                        {
                            // Trace.WriteLine($"Removing nonexistant {mobId} mob from drops");
                            Drops.Remove(kvp.Key);
                            continue;
                        }
                    }

                    Drops[kvp.Key] = kvp.Value.Where(x =>
                    {
                        var itemId = x.ItemID;
                        if (itemId == 0) return true;

                        if (Constants.isEquip(itemId)
                            ? Equips.ContainsKey(itemId)
                            : Items.ContainsKey(itemId)) return true;

                        // Trace.WriteLine($"Removing item {itemId} because it doesnt exist, from {kvp.Key}");
                        return false;
                    }).ToArray();
                }

                // Cleanup map life

                foreach (var map in Maps.Values)
                {
                    var mg = map.MobGen.ToList();
                    foreach (var mgi in mg)
                    {
                        if (Mobs.ContainsKey(mgi.ID)) continue;
                        Console.WriteLine($"Removing mob {mgi.ID} from map {map.ID}, as it does not exist");
                        map.MobGen.Remove(mgi);
                    }

                    var npcs = map.NPC.ToList();
                    foreach (var npc in npcs)
                    {
                        if (NPCs.ContainsKey(npc.ID)) continue;
                        Console.WriteLine($"Removing NPC {npc.ID} from map {map.ID}, as it does not exist");
                        map.NPC.Remove(npc);
                    }


                    foreach (var portal in map.Portals)
                    {
                        if (portal.Value.ToMapID == Constants.InvalidMap) continue;
                        if (!Maps.TryGetValue(portal.Value.ToMapID, out var otherMap))
                        {
                            Console.WriteLine($"Portal {portal.Key} in map {map.ID} points to an unknown map ({portal.Value.ToMapID})");
                        }
                        else if (!otherMap.Portals.ContainsKey(portal.Value.ToName))
                        {
                            Console.WriteLine($"Portal {portal.Key} in map {map.ID} points to an unknown portal (in other map) ({portal.Value.ToMapID}, {portal.Value.ToName})");
                        }
                    }

                    // Normal GMS would start spawning here. We don't care.
                }

                foreach (var mob in Mobs.Values)
                {
                    if (!Drops.ContainsKey("m" + mob.ID))
                    {
                        Trace.WriteLine($"Mob {mob.ID} does not have drops!");
                    }
                    if (mob.Skills != null)
                    {
                        foreach (var mobSkillData in mob.Skills)
                        {
                            if (MobSkills.ContainsKey(mobSkillData.SkillID)) continue;
                            Trace.WriteLine($"Mob {mob.ID} has skill {mobSkillData.SkillID}, but it does not exist!");
                        }
                    }
                }
            }

            Console.WriteLine($"Maps: {Maps.Count}");
            Console.WriteLine($"Mobs: {Mobs.Count}");
            Console.WriteLine($"NPCs: {NPCs.Count}");
            Console.WriteLine($"Eqps: {Equips.Count}");
            Console.WriteLine($"Itms: {Items.Count}");

            FinishInit();
        }

        public static bool HasJob(short JobID)
        {
            if (Jobs.Contains(JobID))
                return true;
            return false;
        }

        static void ReadMobData()
        {
            var mobs = pFile.BaseNode["Mob"].ToList();


            byte GetGroupIdx(string str) => (byte)(str[str.Length - 1] - '1');

            Mobs = IterateAllToDict(mobs, pNode =>
            {
                var data = new MobData();

                data.ID = (int)Utils.ConvertNameToID(pNode.Name);


                var infoNode = pNode["info"];
                var nonInfoNodes = pNode;
                if (infoNode.ContainsChild("link"))
                {
                    var linkedMobId = infoNode["link"].ValueString();

                    Trace.WriteLine($"Mob {data.ID} has a link to mob {linkedMobId}");
                    linkedMobId += ".img";
                    nonInfoNodes = mobs.First(y => y.Name == linkedMobId);
                }


                data.Jumps = nonInfoNodes.ContainsChild("jump");

                foreach (var node in nonInfoNodes)
                {
                    if (!node.ContainsChild("info")) continue;
                    var id = GetGroupIdx(node.Name);
                    var subInfoNode = node["info"];

                    if (node.Name.StartsWith("attack"))
                    {
                        var mad = new MobAttackData();
                        mad.ID = id;

                        foreach (var subNode in subInfoNode)
                        {
                            switch (subNode.Name)
                            {
                                // Effects
                                case "ball":
                                case "hit":
                                case "bulletSpeed":
                                case "attackAfter":
                                case "effect":
                                case "effectAfter":
                                case "jumpAttack": break;

                                case "disease": mad.Disease = subNode.ValueUInt8(); break;
                                case "elemAttr": mad.ElemAttr = subNode.ValueString()[0]; break;
                                case "conMP": mad.MPConsume = subNode.ValueInt16(); break;
                                case "magic": mad.Magic = subNode.ValueBool(); break;
                                case "type": mad.Type = subNode.ValueByte(); break;
                                case "PADamage": mad.PADamage = subNode.ValueInt16(); break;
                                case "level": mad.SkillLevel = subNode.ValueByte(); break;
                                case "range":
                                    if (subNode.ContainsChild("lt"))
                                    {
                                        var lt = subNode["lt"].ValueOrDie<Point>();
                                        mad.RangeLTX = (short)lt.X;
                                        mad.RangeLTY = (short)lt.Y;
                                        var rb = subNode["rb"].ValueOrDie<Point>();
                                        mad.RangeRBX = (short)rb.X;
                                        mad.RangeRBY = (short)rb.Y;
                                    }
                                    else
                                    {
                                        mad.RangeR = subNode["r"].ValueInt16();
                                        var sp = subNode["sp"].ValueOrDie<Point>();
                                        mad.RangeSPX = (short)sp.X;
                                        mad.RangeSPY = (short)sp.Y;
                                    }
                                    break;
                                default:
                                    Console.WriteLine($"Did not handle attack info node {subNode.Name} of mob {data.ID}");
                                    break;
                            }
                        }
                        (data.Attacks = data.Attacks ?? new Dictionary<byte, MobAttackData>()).Add(id, mad);
                    }
                }


                foreach (var node in infoNode)
                {
                    switch (node.Name)
                    {
                        case "link": break;

                        case "level": data.Level = node.ValueByte(); break;
                        case "undead": data.Undead = node.ValueBool(); break;
                        case "bodyAttack": data.BodyAttack = node.ValueBool(); break;
                        case "summonType": data.SummonType = node.ValueByte(); break;
                        case "exp": data.EXP = node.ValueInt32(); break;
                        case "maxHP": data.MaxHP = node.ValueInt32(); break;
                        case "maxMP": data.MaxMP = node.ValueInt32(); break;
                        case "elemAttr": data.elemAttr = node.ValueString(); break;
                        case "PADamage": data.PAD = node.ValueInt32(); break;
                        case "PDDamage": data.PDD = node.ValueInt32(); break;
                        case "MADamage": data.MAD = node.ValueInt32(); break;
                        case "MDDamage": data.MDD = node.ValueInt32(); break;
                        case "eva": data.Eva = node.ValueInt32(); break;
                        case "pushed": data.Pushed = node.ValueBool(); break;
                        case "noregen": data.NoRegen = node.ValueBool(); break;
                        case "invincible": data.Invincible = node.ValueBool(); break;
                        case "selfDestruction": data.SelfDestruction = node.ValueBool(); break;
                        case "firstAttack": data.FirstAttack = node.ValueBool(); break;
                        case "acc": data.Acc = node.ValueInt32(); break;
                        case "publicReward": data.PublicReward = node.ValueBool(); break;
                        case "fs": data.FS = node.ValueFloat(); break;
                        case "flySpeed":
                        case "speed":
                            data.Flies = node.Name == "flySpeed";
                            data.Speed = node.ValueInt16();
                            break;
                        case "revive":
                            data.Revive = node.Select(x => x.ValueInt32()).ToList();
                            break;
                        case "skill":
                            data.Skills = node.Select(skillNode => new MobSkillData
                            {
                                SkillID = skillNode["skill"].ValueByte(),
                                Level = skillNode["level"].ValueByte(),
                                EffectAfter = skillNode["effectAfter"].ValueInt16()
                            }).ToList();

                            break;
                        case "hpRecovery": data.HPRecoverAmount = node.ValueInt32(); break;
                        case "mpRecovery": data.MPRecoverAmount = node.ValueInt32(); break;
                        case "hpTagColor": data.HPTagColor = node.ValueInt32(); break;
                        case "hpTagBgcolor": data.HPTagBgColor = node.ValueInt32(); break;
                        case "boss": data.Boss = node.ValueBool(); break;
                        default:
                            Console.WriteLine($"Did not handle node {node.Name} of mob {data.ID}");
                            break;
                    }
                }

                return data;
            }, x => x.ID);
        }

        static void ReadMapData()
        {
            var maps =
                from region in pFile.BaseNode["Map"]["Map"]
                where region.Name.StartsWith("Map")
                from map in region
                where map.Name != "AreaCode.img"
                select map;

            Maps = IterateAllToDict(maps, p =>
            {
                var mapNode = p;
                var infoNode = mapNode["info"];
                int ID = (int)Utils.ConvertNameToID(mapNode.Name);

                var fieldType = infoNode.ContainsChild("fieldType") ? infoNode["fieldType"].ValueByte() : 0;

                Map map;
                switch (fieldType)
                {
                    case 7: // Snowball entry map 109060001
                    case 4: // Coconut harvest 109080000
                    case 2: // Contimove 101000300
                        map = new Map(ID);
                        break;
                    case 1: // Snowball 109060000
                        map = new Map_Snowball(ID);
                        break;
                    case 0:
                        map = new Map(ID);
                        break;
                    case 6: // JQ maps and such
                        map = new Map_PersonalTimeLimit(ID);
                        break;
                    case 5:
                        Trace.WriteLine("Possible OX quiz? " + ID);
                        map = new Map(ID);
                        break;
                    case 3:
                        map = new Map_Tournament(ID);
                        break; // Tournament 109070000
                    default:
                        throw new Exception($"Unknown FieldType found!!! {fieldType}");
                }

                map.HasClock = mapNode.ContainsChild("clock");

                int VRLeft = 0, VRTop = 0, VRRight = 0, VRBottom = 0;
                foreach (var node in infoNode)
                {
                    switch (node.Name)
                    {
                        case "VRLeft":
                            VRLeft = node.ValueInt32();
                            break;
                        case "VRTop":
                            VRTop = node.ValueInt32();
                            break;
                        case "VRRight":
                            VRRight = node.ValueInt32();
                            break;
                        case "VRBottom":
                            VRBottom = node.ValueInt32();
                            break;

                        case "snow": // ???? Only 1 map
                        case "rain": // ???? Only 1 map
                        case "moveLimit": // Unknown
                        case "mapMark":
                        case "version":
                        case "bgm":
                        case "hideMinimap":
                        case "streetName":
                        case "mapName":
                        case "mapDesc": // Event description (can be empty)?
                        case "help": // More event help? map 101000200 has it
                        case "cloud": // Show clouds in front of the screen
                        case "fs": // Speed thing
                        case "fieldType":
                            break;

                        case "decHP":
                            map.DecreaseHP = node.ValueInt16();
                            break;
                        case "recovery":
                            var amount = node.ValueInt16();
                            // Negative decrease HP
                            map.DecreaseHP = (short)-amount;
                            break;

                        case "timeLimit":
                            map.TimeLimit = node.ValueInt16();
                            break;
                        case "forcedReturn":
                            map.ForcedReturn = node.ValueInt32();
                            break;
                        case "returnMap":
                            map.ReturnMap = node.ValueInt32();
                            break;
                        case "town":
                            map.Town = node.ValueBool();
                            break;
                        case "personalShop":
                            map.AcceptPersonalShop = node.ValueBool();
                            break;
                        case "scrollDisable":
                            map.DisableScrolls = node.ValueBool();
                            break;
                        case "everlast":
                            map.EverlastingDrops = node.ValueBool();
                            break;
                        case "bUnableToShop":
                            map.DisableGoToCashShop = node.ValueBool();
                            break;
                        case "bUnableToChangeChannel":
                            map.DisableChangeChannel = node.ValueBool();
                            break;
                        case "mobRate":
                            map.MobRate = node.ValueFloat();
                            break;
                        case "fieldLimit":
                            map.Limitations = (FieldLimit)node.ValueInt32();
                            break;
                        default:
                            Console.WriteLine($"Unhandled info node {node.Name} for map {ID}");
                            break;

                    }
                }

                if (map.ReturnMap == Constants.InvalidMap)
                {
                    Trace.WriteLine($"No return map for {map.ID}");
                    if (map.ForcedReturn == Constants.InvalidMap)
                    {
                        Trace.WriteLine($"Also no forced return map for {map.ID}");
                    }
                }

                if (map.DisableGoToCashShop) Trace.WriteLine($"Mapid {map.ID}: No cashshop");
                if (map.DisableChangeChannel) Trace.WriteLine($"Mapid {map.ID}: No CC");

                /**************************** HOTFIX??? SEEMS TO WORK PERFECTLY EXCEPT FOR PET PARK **********************************/
                if (map.ForcedReturn == Constants.InvalidMap && (map.Limitations & FieldLimit.SkillLimit) != 0)
                {
                    Program.MainForm.LogDebug($"Found jq map with no forced return. Setting it to nearest town. Map id:{map.ID}");
                    map.ForcedReturn = map.ReturnMap;
                }
                /*********************************************************************************************************************/

                var footholds =
                    from fhlayer in mapNode["foothold"]
                    from fhgroup in fhlayer
                    from fh in fhgroup
                    select new { fh };

                map.SetFootholds(footholds.Select(x => new Foothold
                {
                    ID = (ushort)Utils.ConvertNameToID(x.fh.Name),
                    NextIdentifier = x.fh["next"].ValueUInt16(),
                    PreviousIdentifier = x.fh["prev"].ValueUInt16(),
                    X1 = x.fh["x1"].ValueInt16(),
                    X2 = x.fh["x2"].ValueInt16(),
                    Y1 = x.fh["y1"].ValueInt16(),
                    Y2 = x.fh["y2"].ValueInt16()
                }).ToList());

                map.GenerateMBR(Rectangle.FromLTRB(VRLeft, VRTop, VRRight, VRBottom));

                ReadLife(mapNode, map);
                ReadPortals(mapNode, map);
                ReadSeats(mapNode, map);
                ReadReactors(mapNode, map);

                return map;
            }, x => x.ID);

        }

        static void ReadLife(NXNode mapNode, Map map)
        {
            if (!mapNode.ContainsChild("life")) return;

            foreach (var pNode in mapNode["life"])
            {
                var lifeNode = pNode;
                Life lf = new Life();

                foreach (var node in lifeNode)
                {
                    switch (node.Name)
                    {
                        // Not sure what to do with this one
                        case "hide": break;

                        case "mobTime": lf.RespawnTime = node.ValueInt32(); break;
                        case "f": lf.FacesLeft = node.ValueBool(); break;
                        case "x": lf.X = node.ValueInt16(); break;
                        case "y": lf.Y = node.ValueInt16(); break;
                        case "cy": lf.Cy = node.ValueInt16(); break;
                        case "rx0": lf.Rx0 = node.ValueInt16(); break;
                        case "rx1": lf.Rx1 = node.ValueInt16(); break;
                        case "fh": lf.Foothold = node.ValueUInt16(); break;
                        case "id": lf.ID = (Int32)Utils.ConvertNameToID(node.ValueOrDie<string>()); break;
                        case "type": lf.Type = char.Parse(node.ValueOrDie<string>()); break;
                        case "info": break; // Unknown node. Only 1 NPC has this, with the value 5
                        default:
                            {
                                Console.WriteLine($"Did not handle node {node.Name} of life {lf.ID} node {pNode.Name} map {map.ID}");
                                break;
                            }
                    }
                }
                map.AddLife(lf);
            }
        }

        static void ReadReactors(NXNode mapNode, Map map)
        {
            return; //we handle only with commands for now
            /*for (var layerIndex = 0; layerIndex <= 7; layerIndex++)
            {
                foreach (var objLayerNode in mapNode[layerIndex.ToString()]["obj"])
                {
                    if ((objLayerNode.ContainsChild("reactor") && objLayerNode["reactor"].ValueBool()) ||
                        (objLayerNode.ContainsChild("oS") && objLayerNode["oS"].ValueString() == "Reactor"))
                    {
                        Console.WriteLine("Found reactor under {0}.img/{1}/obj/{2}", map.ID, layerIndex, objLayerNode.Name);
                    }
                }
            }*/
        }

        static void ReadPortals(NXNode mapNode, Map map)
        {
            if (!mapNode.ContainsChild("portal")) return;

            byte idx = 0;
            foreach (var pNode in mapNode["portal"])
            {
                map.AddPortal(new Portal(pNode, idx++));
            }
        }

        static void ReadSeats(NXNode mapNode, Map map)
        {
            if (!mapNode.ContainsChild("seat")) return;

            foreach (var pNode in mapNode["seat"])
            {
                Point pPoint = pNode.ValueOrDie<Point>();

                Seat seat = new Seat
                (
                    (byte)Utils.ConvertNameToID(pNode.Name),
                    (short)pPoint.X,
                    (short)pPoint.Y
                );
                map.AddSeat(seat);
            }
        }


        /*static void ReadQuestData()
        {
            IEnumerator cEnumerator = pFile.ResolvePath("Quest/Check.img").GetEnumerator();
            while (cEnumerator.MoveNext())
            {

                NXNode cNode = (NXNode)cEnumerator.Current;
                IEnumerator pEnumerator = pFile.BaseNode["Quest"]["Check.img"][cNode.Name].GetEnumerator();
                Questdata qd = new Questdata();
                while (pEnumerator.MoveNext())
                {
                    NXNode stageNode = (NXNode)pEnumerator.Current;

                    if (stageNode.ContainsChild("mob"))
                    {
                        qd.Mobs = new List<QuestMob>();
                        IEnumerator enumerable = pFile.BaseNode["Quest"]["Check.img"][cNode.Name][stageNode.Name]["mob"].GetEnumerator();

                        while (enumerable.MoveNext())
                        {
                            NXNode mobNode = (NXNode)enumerable.Current;
                            QuestMob mob = new QuestMob();
                            mob.ReqKills = pFile.BaseNode["Quest"]["Check.img"][cNode.Name][stageNode.Name]["mob"][mobNode.Name]["count"].ValueInt32();
                            mob.MobID = pFile.BaseNode["Quest"]["Check.img"][cNode.Name][stageNode.Name]["mob"][mobNode.Name]["id"].ValueInt32();
                            qd.Mobs.Add(mob);
                        }
                    }
                }
                Quests.Add(int.Parse(cNode.Name), qd);
            }

            IEnumerator enumerator = pFile.ResolvePath("Quest/Act.img").GetEnumerator();
            while (enumerator.MoveNext())
            {
                NXNode pNode = (NXNode)enumerator.Current;
                IEnumerator pEnumerator = pFile.BaseNode["Quest"]["Act.img"][pNode.Name].GetEnumerator();
                Questdata qd = new Questdata();
                while (pEnumerator.MoveNext())
                {
                    NXNode iNode = (NXNode)pEnumerator.Current;
                    qd.Stage = byte.Parse(iNode.Name);

                    if (qd.Stage == 0)
                    {
                        if (iNode.ContainsChild("item"))
                        {
                            NXNode bNode = (NXNode)pEnumerator.Current;
                            IEnumerator sEnumerator = pFile.BaseNode["Quest"]["Act.img"][pNode.Name][iNode.Name]["item"].GetEnumerator();

                            while (sEnumerator.MoveNext())
                            {
                                NXNode lNode = (NXNode)sEnumerator.Current;
                                qd.ReqItems = new List<ItemReward>();
                                ItemReward ir = new ItemReward();
                                ir.ItemRewardCount = pFile.BaseNode["Quest"]["Act.img"][pNode.Name][iNode.Name]["item"][lNode.Name]["count"].ValueInt16();
                                ir.Reward = pFile.BaseNode["Quest"]["Act.img"][pNode.Name][iNode.Name]["item"][lNode.Name]["id"].ValueInt32();
                                qd.ReqItems.Add(ir);

                            }
                        }
                    }
                    else
                    {
                        if (iNode.ContainsChild("item"))
                        {
                            NXNode bNode = (NXNode)pEnumerator.Current;
                            IEnumerator sEnumerator = pFile.BaseNode["Quest"]["Act.img"][pNode.Name][iNode.Name]["item"].GetEnumerator();
                            qd.ItemRewards = new List<ItemReward>();
                            qd.RandomRewards = new List<ItemReward>();
                            while (sEnumerator.MoveNext())
                            {
                                NXNode lNode = (NXNode)sEnumerator.Current;

                                if (lNode.ContainsChild("prop"))
                                {

                                    ItemReward ir = new ItemReward();
                                    ir.ItemRewardCount = pFile.BaseNode["Quest"]["Act.img"][pNode.Name][iNode.Name]["item"][lNode.Name]["count"].ValueInt16();
                                    ir.Reward = pFile.BaseNode["Quest"]["Act.img"][pNode.Name][iNode.Name]["item"][lNode.Name]["id"].ValueInt32();
                                    qd.RandomRewards.Add(ir);
                                }
                                else
                                {

                                    ItemReward ir = new ItemReward();
                                    ir.ItemRewardCount = pFile.BaseNode["Quest"]["Act.img"][pNode.Name][iNode.Name]["item"][lNode.Name]["count"].ValueInt16();
                                    ir.Reward = pFile.BaseNode["Quest"]["Act.img"][pNode.Name][iNode.Name]["item"][lNode.Name]["id"].ValueInt32();
                                    qd.ItemRewards.Add(ir);
                                }
                            }
                        }
                    }

                    if (iNode.ContainsChild("exp"))
                    {
                        qd.ExpReward = pFile.BaseNode["Quest"]["Act.img"][pNode.Name][iNode.Name]["exp"].ValueInt32();
                    }
                    if (iNode.ContainsChild("money"))
                    {
                        qd.MesoReward = pFile.BaseNode["Quest"]["Act.img"][pNode.Name][iNode.Name]["money"].ValueInt32();
                    }
                }

                if (pNode.Name == "1008")
                {
                    ////Console.WriteLine("MESO REWARD : " + qd.MesoReward);
                }
                if (Quests.ContainsKey(int.Parse(pNode.Name)))
                {
                    Quests[int.Parse(pNode.Name)].ExpReward = qd.ExpReward;
                    Quests[int.Parse(pNode.Name)].MesoReward = qd.MesoReward;
                    Quests[int.Parse(pNode.Name)].RandomRewards = qd.RandomRewards;
                    Quests[int.Parse(pNode.Name)].ItemRewards = qd.ItemRewards;
                    Quests[int.Parse(pNode.Name)].ReqItems = qd.ReqItems;
                }
                else
                {
                    Quests.Add(int.Parse(pNode.Name), qd);
                }
            }
        }*/

        static void ReadNpcs()
        {
            var npcs = pFile.BaseNode["Npc"].ToList();

            NPCs = IterateAllToDict(npcs, pNode =>
            {
                var infoNode = pNode["info"];

                NPCData npc = new NPCData();
                int ID = (int)Utils.ConvertNameToID(pNode.Name);
                npc.ID = ID;
                npc.Shop = new List<ShopItemData>();
                
                var nonInfoNodes = pNode;
                if (infoNode.ContainsChild("link"))
                {
                    var linkedNpcID = infoNode["link"].ValueString();

                    Trace.WriteLine($"NPC {npc.ID} has a link to NPC {linkedNpcID}");
                    linkedNpcID += ".img";
                    nonInfoNodes = npcs.First(y => y.Name == linkedNpcID);
                }

                foreach (var node in infoNode)
                {
                    switch (node.Name)
                    {
                        case "hideName":
                        case "link":
                        case "float": // Floating NPC
                        case "default": // Icon used for npc chat dialog, see NPC 2030006
                        case "dcTop": // Double Click mark
                        case "dcRight":
                        case "dcBottom":
                        case "dcLeft":
                        case "dcMark": break;
                        case "reg":
                            {
                                // subnodes /varset /varget variable for the NPC

                                foreach (var regNode in node)
                                {
                                    Console.WriteLine($"reg node {regNode.Name} in npc {npc.ID}");
                                }
                                break;
                            }
                        case "quest":
                            npc.Quest = node.ValueString();
                            break;
                        case "trunk":
                            npc.Trunk = node.ValueInt32();
                            break;
                        case "speed":
                            npc.Speed = node.ValueInt16();
                            break;
                        case "speak":
                            npc.SpeakLineCount = (byte)node.ChildCount;
                            break;
                        case "shop":
                            {
                                foreach (var iNode in node)
                                {
                                    ShopItemData item = new ShopItemData()
                                    {
                                        ID = (int)Utils.ConvertNameToID(iNode.Name)
                                    };
                                    foreach (var subNode in iNode)
                                    {
                                        switch (subNode.Name)
                                        {
                                            case "period":
                                                item.Period = subNode.ValueByte();
                                                break;
                                            case "price":
                                                item.Price = subNode.ValueInt32();
                                                break;
                                            case "stock":
                                                item.Stock = subNode.ValueInt32();
                                                break;
                                            case "unitPrice":
                                                item.UnitRechargeRate = subNode.ValueFloat();
                                                break;
                                            default:
                                                Console.WriteLine($"Unhandled node {subNode.Name} in shop of NPC {npc.ID}");
                                                break;
                                        }
                                    }

                                    npc.Shop.Add(item);
                                }
                                break;
                            }
                        default:
                            Console.WriteLine($"Unhandled node {node.Name} for NPC {npc.ID}");
                            break;
                    }
                }

                return npc;
            }, x => x.ID);
        }

        static void ReadSkills()
        {
            var allJobs = pFile.BaseNode["Skill"].Where(x => x.Name != "MobSkill.img").ToList();

            Jobs = IterateAllToDict(allJobs, pNode => int.Parse(pNode.Name.Replace(".img", "")), x => x).Values.ToList();


            Skills = IterateAllToDict(allJobs.SelectMany(x => x["skill"]), mNode =>
            {
                int SkillID = int.Parse(mNode.Name);
                var skillData = new SkillData
                {
                    ID = SkillID
                };

                var elementFlags = SkillElement.Normal;

                if (mNode.ContainsChild("elemAttr"))
                {
                    string pbyte = mNode["elemAttr"].ValueString();
                    switch (pbyte.ToLowerInvariant())
                    {
                        case "i":
                            elementFlags = SkillElement.Ice;
                            break;
                        case "f":
                            elementFlags = SkillElement.Fire;
                            break;
                        case "s":
                            elementFlags = SkillElement.Poison;
                            break;
                        case "l":
                            elementFlags = SkillElement.Lightning;
                            break;
                        case "h":
                            elementFlags = SkillElement.Holy;
                            break;
                        default:
                            Console.WriteLine($"Unhandled elemAttr type {pbyte} for id {SkillID}");
                            break;
                    }
                }

                skillData.Element = elementFlags;

                foreach (var iNode in mNode)
                {
                    switch (iNode.Name)
                    {
                        case "damage": // Crit hit for 4100001
                        case "summon":
                        case "special":

                        case "Frame":
                        case "cDoor":
                        case "mDoor":

                        case "prepare":
                        case "mob":
                        case "ball":
                        case "ball0":
                        case "ball1":
                        case "action":
                        case "affected":
                        case "effect":
                        case "effect0":
                        case "effect1":
                        case "effect2":
                        case "effect3":
                        case "finish": // him
                        case "hit":
                        case "icon":
                        case "iconMouseOver":
                        case "iconDisabled":
                        case "afterimage":
                        case "tile":
                        case "level":
                        case "state":
                        case "mob0":
                        case "finalAttack": break;

                        case "elemAttr":
                            string pbyte = iNode.ValueString();
                            switch (pbyte.ToLowerInvariant())
                            {
                                case "i":
                                    elementFlags = SkillElement.Ice;
                                    break;
                                case "f":
                                    elementFlags = SkillElement.Fire;
                                    break;
                                case "s":
                                    elementFlags = SkillElement.Poison;
                                    break;
                                case "l":
                                    elementFlags = SkillElement.Lightning;
                                    break;
                                case "h":
                                    elementFlags = SkillElement.Holy;
                                    break;
                                default:
                                    Console.WriteLine($"Unhandled elemAttr type {pbyte} for id {SkillID}");
                                    break;
                            }
                            break;

                        case "skillType":
                            skillData.Type = iNode.ValueByte();
                            break;
                        case "weapon":
                            skillData.Weapon = iNode.ValueByte();
                            break;

                        case "req":
                            skillData.RequiredSkills = new Dictionary<int, byte>();
                            foreach (var nxNode in iNode)
                            {
                                skillData.RequiredSkills[int.Parse(nxNode.Name)] = nxNode.ValueByte();
                            }
                            break;
                        default:
                            Trace.WriteLine($"Unknown skill prop {iNode.Name} in {SkillID}");
                            break;
                    }

                }

                skillData.Levels = new SkillLevelData[mNode["level"].ChildCount + 1];
                foreach (var iNode in mNode["level"])
                {
                    var sld = new SkillLevelData();

                    foreach (var nxNode in iNode)
                    {
                        switch (nxNode.Name)
                        {
                            case "hs": // help string (refer to Strings.wz)
                            case "action": // Stance
                            case "ball":
                            case "hit":
                            case "bulletConsume": // Avenger uses like 3 stars
                            case "z": break;

                            case "x":
                                sld.XValue = nxNode.ValueInt16();
                                break;
                            case "y":
                                sld.YValue = nxNode.ValueInt16();
                                break;
                            case "attackCount":
                                sld.HitCount = nxNode.ValueByte();
                                break;
                            case "mobCount":
                                sld.MobCount = nxNode.ValueByte();
                                break;
                            case "time":
                                sld.BuffTime = nxNode.ValueInt32();
                                break;
                            case "damage":
                                sld.Damage = nxNode.ValueInt16();
                                break;
                            case "range":
                                sld.AttackRange = nxNode.ValueInt16();
                                break;
                            case "mastery":
                                sld.Mastery = nxNode.ValueByte();
                                break;
                            case "hp":
                                sld.HPProperty = nxNode.ValueInt16();
                                break;
                            case "mp":
                                sld.MPProperty = nxNode.ValueInt16();
                                break;
                            case "prop":
                                sld.Property = nxNode.ValueInt16();
                                break;
                            case "hpCon":
                                sld.HPUsage = nxNode.ValueInt16();
                                break;
                            case "mpCon":
                                sld.MPUsage = nxNode.ValueInt16();
                                break;
                            case "itemCon":
                                sld.ItemIDUsage = nxNode.ValueInt32();
                                break;
                            case "itemConNo":
                                sld.ItemAmountUsage = nxNode.ValueInt16();
                                break;
                            case "bulletCount":
                                sld.BulletUsage = nxNode.ValueInt16();
                                break;
                            case "moneyCon":
                                sld.MesosUsage = nxNode.ValueInt16();
                                break;
                            case "speed":
                                sld.Speed = nxNode.ValueInt16();
                                break;
                            case "jump":
                                sld.Jump = nxNode.ValueInt16();
                                break;
                            case "eva":
                                sld.Avoidability = nxNode.ValueInt16();
                                break;
                            case "acc":
                                sld.Accurancy = nxNode.ValueInt16();
                                break;
                            case "mad":
                                sld.MagicAttack = nxNode.ValueInt16();
                                break;
                            case "mdd":
                                sld.MagicDefense = nxNode.ValueInt16();
                                break;
                            case "pad":
                                sld.WeaponAttack = nxNode.ValueInt16();
                                break;
                            case "pdd":
                                sld.WeaponDefense = nxNode.ValueInt16();
                                break;
                            case "lt":
                                {
                                    Point pPoint = nxNode.ValueOrDie<Point>();
                                    sld.LTX = (short)pPoint.X;
                                    sld.LTY = (short)pPoint.Y;
                                    break;
                                }
                            case "rb":
                                {
                                    Point pPoint = nxNode.ValueOrDie<Point>();
                                    sld.RBX = (short)pPoint.X;
                                    sld.RBY = (short)pPoint.Y;
                                    break;
                                }

                            default:
                                Console.WriteLine($"Unhandled skill level node {nxNode.Name} for id {SkillID}");
                                break;
                        }
                    }

                    sld.ElementFlags = elementFlags;
                    if (SkillID == Constants.Gm.Skills.Hide)
                    {
                        // Give hide some time... like lots of hours
                        sld.BuffTime = 24 * 60 * 60;
                        sld.XValue = 1; // Eh. Otherwise there's no buff
                    }

                    skillData.Levels[byte.Parse(iNode.Name)] = sld;
                }
                skillData.MaxLevel = (byte)(skillData.Levels.Length - 1); // As we skip 0

                return skillData;
            }, x => x.ID);

            MobSkills = IterateAllToDict(pFile.BaseNode["Skill"]["MobSkill.img"], eNode =>
            {
                var dict = new Dictionary<byte, MobSkillLevelData>();
                byte SkillID = (byte)int.Parse(eNode.Name);
                foreach (var sNode in eNode["level"])
                {
                    var levelNode = sNode;

                    byte Level = (byte)int.Parse(sNode.Name);
                    MobSkillLevelData msld = new MobSkillLevelData()
                    {
                        SkillID = SkillID,
                        Level = Level
                    };

                    foreach (var node in levelNode)
                    {
                        switch (node.Name)
                        {
                            case "effect":
                            case "affected":
                            case "tile": // Clouds (for the poison cloud skill)
                            case "mob":
                            case "mob0": break;

                            case "time":
                                msld.Time = node.ValueInt16();
                                break;
                            case "mpCon":
                                msld.MPConsume = node.ValueInt16();
                                break;
                            case "x":
                                msld.X = node.ValueInt32();
                                break;
                            case "y":
                                msld.Y = node.ValueInt32();
                                break;
                            case "prop":
                                msld.Prop = node.ValueByte();
                                break;
                            case "interval":
                                msld.Cooldown = node.ValueInt16();
                                break;
                            case "hp":
                                msld.HPLimit = node.ValueByte();
                                break;
                            case "limit":
                                msld.SummonLimit = node.ValueUInt16();
                                break;
                            case "summonEffect":
                                msld.SummonEffect = node.ValueByte();
                                break;
                            case "lt":
                                {
                                    Point pPoint = node.ValueOrDie<Point>();
                                    msld.LTX = (short)pPoint.X;
                                    msld.LTY = (short)pPoint.Y;
                                    break;
                                }
                            case "rb":
                                {
                                    Point pPoint = node.ValueOrDie<Point>();
                                    msld.RBX = (short)pPoint.X;
                                    msld.RBY = (short)pPoint.Y;
                                    break;
                                }

                            default:
                                {
                                    if (node.Name.All(char.IsDigit))
                                    {
                                        var summonId = node.ValueInt32();
                                        (msld.Summons ?? (msld.Summons = new List<int>())).Add(summonId);
                                    }
                                    else
                                    {
                                        Console.WriteLine(
                                            $"Unhandled Mob skill {msld.SkillID} level {msld.Level} node {node.Name}");
                                    }
                                    break;
                                }
                        }
                    }


                    dict[msld.Level] = msld;
                }

                return new Tuple<byte, Dictionary<byte, MobSkillLevelData>>(SkillID, dict);
            }, x => x.Item1, x => x.Item2);
        }

        static void ReadQuiz()
        {
            var questions = from page in pFile.BaseNode["Etc"]["OXQuiz.img"]
                       from question in page
                       select new QuizData(byte.Parse(page.Name), byte.Parse(question.Name), question["a"].ValueByte() == 0 ? 'x' : 'o');

            var pages = questions
                .GroupBy(q => q.QuestionPage)
                .Where(p => p.Key < 8); //pages past 7 are untranslated korean in this version

            foreach (var page in pages)
            {
                QuizQuestions[page.Key] = page.ToList();
            }
        }

        static int CalculateDropChance(double x)
        {
            if (x > 1.0 || x < 0.0)
                throw new Exception("Invalid dropchance");


            x *= 1000000000.0;
            var y = Math.Min((int)x, 1000000000);

            return y;
        }

        static void ReadDrops()
        {
            Drops = IterateAllToDict(pDropFile.BaseNode["Reward_ori.img"], pNode =>
            {
                string dropper = pNode.Name;

                var drops = pNode.Select(iNode =>
                    {
                        var dropdata = new DropData();
                        dropdata.DateExpire = DateTime.MaxValue;

                        foreach (var node in iNode)
                        {
                            switch (node.Name)
                            {
                                case "period":
                                    dropdata.Period = node.ValueUInt16();
                                    break;
                                case "dateExpire":
                                    int val = node.ValueInt32();
                                    int year = val / 1000000;
                                    int month = val / 10000 % 100;
                                    int day = val / 100 % 100;
                                    int hour = val % 100;

                                    dropdata.DateExpire = new DateTime(year, month, day, hour, 0, 0);
                                    break;

                                case "money":
                                    dropdata.Mesos = node.ValueInt32();
                                    break;
                                case "item":
                                    dropdata.ItemID = node.ValueInt32();
                                    break;
                                case "min":
                                    dropdata.Min = node.ValueInt16();
                                    break;
                                case "max":
                                    dropdata.Max = node.ValueInt16();
                                    break;
                                case "premium":
                                    dropdata.Premium = node.ValueBool();
                                    break;
                                case "prob":
                                    dropdata.Chance = CalculateDropChance(node.ValueDouble());
                                    break;
                                default:
                                    Console.WriteLine($"Unhandled node {node.Name} in drop {dropper}");
                                    break;
                            }
                        }
                        return dropdata;

                    }
                ).ToArray();

                if (dropper.StartsWith("m"))
                {
                    string trimmed = dropper.Trim().StartsWith("m0") ? dropper.Trim().Replace("m0", "m") : dropper;

                    return new Tuple<string, DropData[]>(trimmed, drops);
                }
                else if (dropper.StartsWith("r"))
                {
                    string trimmed = dropper.Trim().StartsWith("r000") ? dropper.Trim().Replace("r000", "r") : dropper;
                    return new Tuple<string, DropData[]>(trimmed, drops);
                }
                else
                {
                    Console.WriteLine("Unknown dropper type? {0}", dropper);
                    return new Tuple<string, DropData[]>(dropper, drops);
                }
            }, x => x.Item1, x => x.Item2);

            // AddThanksgivingDrops(); //TODO remove this after thanksgiving
        }

        private static void AddThanksgivingDrops() //Mother fucking turkeys?
        {
            List<DropData> tdaydrops = new List<int>() { 03994012, 03994000, 03994006, 03994003, 03994001, 03994013, 03994008, 03994005, 03994007, 03994010 }.Select(id => new DropData()
            {
                ItemID = id,
                Chance = CalculateDropChance((id == 03994012 || id == 03994013) ? 0.00007 : 0.00015)
            }).ToList();

            var fuck = Drops.Select(dropList =>
            {
                var newlist = dropList.Value.ToList();
                newlist.AddRange(tdaydrops);
                return new KeyValuePair<string, DropData[]>(dropList.Key, newlist.ToArray());
            });
            fuck.ForEach(shit => { Drops.Remove(shit.Key); Drops.Add(shit.Key, shit.Value); });
        }



    }
}