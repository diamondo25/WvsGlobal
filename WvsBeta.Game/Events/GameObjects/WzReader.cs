using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace WvsBeta.Game {
	enum ParseTypes : byte {
		Maps = 0,
		Equips,
		NPCs,
		Items,
		Mobs,
		Skills,
		Drops,
		Pets,
		Cash
	}

	enum ConsumeCurseTypes : byte {
		Curse = 0x01,
		Seal = 0x02,
		Weakness = 0x04,
		Darkness = 0x08,
		Poison = 0x10
	}

	public class DataProvider {
		private static BinaryReader Reader { get; set; }
		public static Dictionary<int, Map> Maps { get; set; }
		public static Dictionary<int, EquipData> Equips { get; set; }
		public static Dictionary<int, NPCData> NPCs { get; set; }
		public static Dictionary<int, ItemData> Items { get; set; }
		public static Dictionary<int, PetData> Pets { get; set; }
		public static Dictionary<int, MobData> Mobs { get; set; }
		public static Dictionary<int, Dictionary<byte, SkillLevelData>> Skills { get; set; }
		public static Dictionary<byte, Dictionary<byte, MobSkillLevelData>> MobSkills { get; set; }
		public static Dictionary<string, List<DropData>> Drops { get; set; }

		public static void Load(string pPath) {
			if (!File.Exists(pPath)) throw new FileNotFoundException();
            using (Reader = new BinaryReader(File.Open(pPath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                while (Reader.BaseStream.Position < Reader.BaseStream.Length)
                {
                    ParseTypes Type = (ParseTypes)Reader.ReadByte();
                    switch (Type)
                    {
                        case ParseTypes.Maps:
                            Console.Write("Starting reading maps...");
                            Maps = new Dictionary<int, Map>();
                            ReadMaps();
                            Console.WriteLine("Done.");
                            break;
                        case ParseTypes.Equips:
                            Console.Write("Starting reading equips...");
                            Equips = new Dictionary<int, EquipData>();
                            ReadEquips();
                            Console.WriteLine("Done.");
                            break;
                        case ParseTypes.NPCs:
                            Console.Write("Starting reading ncps...");
                            NPCs = new Dictionary<int, NPCData>();
                            ReadNPCs();
                            Console.WriteLine("Done.");
                            break;
                        case ParseTypes.Items:
                            Console.Write("Starting reading items...");
                            Items = new Dictionary<int, ItemData>();
                            ReadItems();
                            Console.WriteLine("Done.");
                            break;
                        case ParseTypes.Mobs:
                            Console.Write("Starting reading mobs...");
                            Mobs = new Dictionary<int, MobData>();
                            ReadMobs();
                            Console.WriteLine("Done.");
                            break;
                        case ParseTypes.Skills:
                            Console.Write("Starting reading skills...");
                            Skills = new Dictionary<int, Dictionary<byte, SkillLevelData>>();
                            MobSkills = new Dictionary<byte, Dictionary<byte, MobSkillLevelData>>();
                            ReadSkills();
                            Console.WriteLine("Done.");
                            break;
                        case ParseTypes.Drops:
                            Console.Write("Starting reading drops...");
                            Drops = new Dictionary<string, List<DropData>>();
                            ReadDrops();
                            Console.WriteLine("Done.");
                            break;
                        case ParseTypes.Pets:
                            Console.Write("Starting reading pets...");
                            Pets = new Dictionary<int, PetData>();
                            ReadPets();
                            Console.WriteLine("Done.");
                            break;
                        default:
                            throw new OutOfMemoryException("Could not find parsetype.");
                    }
                }
            }
            Reader = null; // Just to be sure it's dereferenced
		}

		private static void ReadPets() {
			short Count = Reader.ReadInt16();
			for (short i = 0; i < Count; i++) {
				int petid = Reader.ReadInt32();
				PetData pd = new PetData();
				pd.ItemID = petid;
				pd.Hungry = Reader.ReadByte();
				pd.Life = Reader.ReadByte();
				pd.Reactions = new Dictionary<byte, PetReactionData>();

				byte interacts = Reader.ReadByte();
				for (byte j = 0; j < interacts; j++) {
					PetReactionData prd = new PetReactionData();
					prd.ReactionID = Reader.ReadByte();
					prd.Inc = Reader.ReadByte();
					prd.Prob = Reader.ReadByte();
					pd.Reactions.Add(prd.ReactionID, prd);
				}
				Pets.Add(petid, pd);
			}
		}

		private static void ReadDrops() {
			ushort Count = Reader.ReadUInt16();
			for (ushort i = 0; i < Count; i++) {
				string dropper = Reader.ReadString();
				Drops.Add(dropper, new List<DropData>());
				short dropamount = Reader.ReadInt16();
				for (byte j = 0; j < dropamount; j++) {
					DropData dropdata = new DropData();
					dropdata.ItemID = Reader.ReadInt32();
					dropdata.Mesos = Reader.ReadInt32();
					dropdata.Min = Reader.ReadInt16();
					dropdata.Max = Reader.ReadInt16();
					dropdata.Chance = Reader.ReadInt32();
					Drops[dropper].Add(dropdata);
				}
			}
		}

		private static void ReadSkills() {
			short Count = Reader.ReadInt16();
			for (short i = 0; i < Count; i++) {
				int skillid = Reader.ReadInt32();
				Skills.Add(skillid, new Dictionary<byte, SkillLevelData>());
				byte levels = Reader.ReadByte();
				for (byte j = 0; j < levels; j++) {
					byte level = Reader.ReadByte();
					SkillLevelData sld = new SkillLevelData();
					sld.MobCount = Reader.ReadByte();
					sld.HitCount = Reader.ReadByte();

					sld.BuffTime = Reader.ReadInt16();
					sld.Damage = Reader.ReadInt16();
					sld.AttackRange = Reader.ReadInt16();
					sld.Mastery = Reader.ReadByte();

					sld.HPProperty = Reader.ReadInt16();
					sld.MPProperty = Reader.ReadInt16();
					sld.Property = Reader.ReadInt16();

					sld.HPUsage = Reader.ReadInt16();
					sld.MPUsage = Reader.ReadInt16();
					sld.ItemIDUsage = Reader.ReadInt32();
					sld.ItemAmountUsage = Reader.ReadInt16();
					sld.BulletUsage = Reader.ReadInt16();
					sld.MesosUsage = Reader.ReadInt16();

					sld.XValue = Reader.ReadInt16();
					sld.YValue = Reader.ReadInt16();

					sld.Speed = Reader.ReadInt16();
					sld.Jump = Reader.ReadInt16();
					sld.WeaponAttack = Reader.ReadInt16();
					sld.MagicAttack = Reader.ReadInt16();
					sld.WeaponDefence = Reader.ReadInt16();
					sld.MagicDefence = Reader.ReadInt16();
					sld.Accurancy = Reader.ReadInt16();
					sld.Avoidability = Reader.ReadInt16();

					sld.LTX = Reader.ReadInt16();
					sld.LTY = Reader.ReadInt16();
					sld.RBX = Reader.ReadInt16();
					sld.RBY = Reader.ReadInt16();
					Skills[skillid].Add(level, sld);
				}
			}

			byte amount = Reader.ReadByte();
			for (byte i = 0; i < amount; i++) {
				byte skillid = Reader.ReadByte();
				MobSkills.Add(skillid, new Dictionary<byte, MobSkillLevelData>());
				byte levels = Reader.ReadByte();
				for (byte j = 0; j < levels; j++) {
					MobSkillLevelData msld = new MobSkillLevelData();
					msld.SkillID = skillid;

					msld.Level = Reader.ReadByte();
					msld.Time = Reader.ReadInt16();
					msld.MPConsume = Reader.ReadInt16();
					msld.X = Reader.ReadInt32();
					msld.Y = Reader.ReadInt32();
					msld.Prop = Reader.ReadByte();
					msld.Cooldown = Reader.ReadInt16();

					msld.LTX = Reader.ReadInt16();
					msld.LTY = Reader.ReadInt16();
					msld.RBX = Reader.ReadInt16();
					msld.RBY = Reader.ReadInt16();

					msld.HPLimit = Reader.ReadByte();
					msld.SummonLimit = Reader.ReadByte();
					msld.SummonEffect = Reader.ReadByte();
					byte summons = Reader.ReadByte();
					msld.Summons = new List<int>();
					for (byte k = 0; k < summons; k++) {
						msld.Summons.Add(Reader.ReadInt32());
					}
					MobSkills[skillid].Add(msld.Level, msld);
				}
			}
		}

		private static void ReadMobs() {
			int Count = Reader.ReadInt32();
			for (int i = 0; i < Count; i++) {
				MobData data = new MobData();
				data.ID = Reader.ReadInt32();
				data.Level = Reader.ReadByte();
				data.Boss = Reader.ReadBoolean();
				data.Undead = Reader.ReadBoolean();
				data.BodyAttack = Reader.ReadBoolean();
				data.SummonType = Reader.ReadByte();
				data.EXP = Reader.ReadInt32();
				data.MaxHP = Reader.ReadInt32();
				data.MaxMP = Reader.ReadInt32();
				data.HPRecoverAmount = Reader.ReadInt32();
				data.MPRecoverAmount = Reader.ReadInt32();
				data.Speed = Reader.ReadInt16();

				byte summons = Reader.ReadByte();
				data.Revive = new List<int>();
				for (byte j = 0; j < summons; j++) {
					data.Revive.Add(Reader.ReadInt32());
				}

				byte skills = Reader.ReadByte();
				data.Skills = new List<MobSkillData>();
				for (byte j = 0; j < skills; j++) {
					MobSkillData msd = new MobSkillData();
					msd.SkillID = (byte)Reader.ReadInt16();
					msd.Level = Reader.ReadByte();
					msd.EffectAfter = Reader.ReadInt16();
					data.Skills.Add(msd);
				}

				byte attacks = Reader.ReadByte();
				data.Attacks = new Dictionary<byte, MobAttackData>();
				for (byte j = 0; j < attacks; j++) {
					MobAttackData mad = new MobAttackData();
					mad.ID = j;//Reader.ReadByte();
					mad.MPConsume = (short)Reader.ReadUInt16();
					mad.MPBurn = (short)Reader.ReadUInt16();
					mad.SkillID = (short)Reader.ReadUInt16();
					mad.SkillLevel = Reader.ReadByte();
					data.Attacks.Add(mad.ID, mad);
				}
				Mobs.Add(data.ID, data);
			}
		}

		private static void ReadItems() {
			ushort Count = Reader.ReadUInt16();
			for (ushort i = 0; i < Count; i++) {
				ItemData item = new ItemData();
				item.ID = Reader.ReadInt32();
				bool hasInfo = Reader.ReadBoolean();
				if (hasInfo) {
					item.Price = Reader.ReadInt32();
					item.Cash = Reader.ReadBoolean();
					item.MaxSlot = Reader.ReadUInt16();
					item.Mesos = Reader.ReadInt32();

					item.ScrollSuccessRate = Reader.ReadByte();
					item.ScrollCurseRate = Reader.ReadByte();
					item.IncStr = Reader.ReadByte();
					item.IncDex = Reader.ReadByte();
					item.IncInt = Reader.ReadByte();
					item.IncLuk = Reader.ReadByte();
					item.IncMHP = Reader.ReadByte();
					item.IncMMP = Reader.ReadByte();
					item.IncWAtk = Reader.ReadByte();
					item.IncMAtk = Reader.ReadByte();
					item.IncWDef = Reader.ReadByte();
					item.IncMDef = Reader.ReadByte();
					item.IncAcc = Reader.ReadByte();
					item.IncAvo = Reader.ReadByte();
					item.IncJump = Reader.ReadByte();
					item.IncSpeed = Reader.ReadByte();
				}
				else {
					item.Price = 0;
					item.Cash = false;
					item.MaxSlot = 1;
					item.Mesos = 0;

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
				}

				hasInfo = Reader.ReadBoolean();
				if (hasInfo) {

					item.MoveTo = Reader.ReadInt32();

					item.CureFlags = Reader.ReadByte();

					item.HP = Reader.ReadInt16();
					item.MP = Reader.ReadInt16();
					item.HPRate = Reader.ReadInt16();
					item.MPRate = Reader.ReadInt16();
					item.Speed = Reader.ReadInt16();
					item.Avoidance = Reader.ReadInt16();
					item.Accuracy = Reader.ReadInt16();
					item.MagicAttack = Reader.ReadInt16();
					item.WeaponAttack = Reader.ReadInt16();
					item.BuffTime = Reader.ReadInt32();
				}
				else {
					item.MoveTo = 0;
					item.CureFlags = 0;
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

				item.Summons = new List<ItemSummonInfo>();
				byte amount = Reader.ReadByte();
				for (int s = 0; s < amount; s++) {
					ItemSummonInfo isi = new ItemSummonInfo();
					isi.MobID = Reader.ReadInt32();
					isi.Chance = Reader.ReadByte();
					item.Summons.Add(isi);
				}


				Items.Add(item.ID, item);
			}
		}

		#region NPC
		private static void ReadNPCs() {
			ushort Count = Reader.ReadUInt16();
			for (ushort i = 0; i < Count; i++) {
				NPCData npc = new NPCData();
				npc.ID = Reader.ReadInt32();
				npc.Quest = ReadString();
				npc.Trunk = Reader.ReadInt16();
				npc.Shop = new List<ShopItemData>();
				byte ItemCount = Reader.ReadByte();
				for (byte iw = 0; iw < ItemCount; iw++) {
					ShopItemData item = new ShopItemData();
					item.ID = Reader.ReadInt32();
					item.Price = Reader.ReadInt32();
					item.Stock = Reader.ReadInt32();
					npc.Shop.Add(item);
				}
				NPCs.Add(npc.ID, npc);
			}
		}
		#endregion

		private static void ReadEquips() {
			ushort Count = Reader.ReadUInt16();
			for (ushort i = 0; i < Count; i++) {
				EquipData eq = new EquipData();
				eq.ID = Reader.ReadInt32();
				eq.isCash = Reader.ReadBoolean();
				eq.Type = ReadString();
				eq.RequiredLevel = Reader.ReadByte();
				eq.Scrolls = (byte)Reader.ReadUInt16();
				eq.RequiredDexterity = Reader.ReadUInt16();
				eq.RequiredIntellect = Reader.ReadUInt16();
				eq.RequiredLuck = Reader.ReadUInt16();
				eq.RequiredStrength = Reader.ReadUInt16();
				eq.RequiredJob = Reader.ReadUInt16();
				eq.Price = Reader.ReadInt32();
				eq.Strength = Reader.ReadInt16();
				eq.Dexterity = Reader.ReadInt16();
				eq.Intellect = Reader.ReadInt16();
				eq.Luck = Reader.ReadInt16();
				eq.MagicDefense = Reader.ReadByte();
				eq.WeaponDefense = Reader.ReadByte();
				eq.WeaponAttack = Reader.ReadByte();
				eq.MagicAttack = Reader.ReadByte();
				eq.Speed = Reader.ReadByte();
				eq.Jump = Reader.ReadByte();
				eq.Accuracy = Reader.ReadByte();
				eq.Avoidance = Reader.ReadByte();
				eq.HP = Reader.ReadInt16();
				eq.MP = Reader.ReadInt16();
				Equips.Add(eq.ID, eq);
			}
		}
		#region MapData

		private static void ReadMaps() {
			ushort Count = Reader.ReadUInt16();
			for (ushort i = 0; i < Count; ++i) {
				Map map = new Map(Reader.ReadInt32());
				map.ForcedReturn = Reader.ReadInt32();
				map.ReturnMap = Reader.ReadInt32();
				map.Town = Reader.ReadBoolean();
				map.FieldType = Reader.ReadByte();
				map.HasClock = Reader.ReadBoolean();
				map.MobRate = Reader.ReadSingle();
				Maps.Add(map.ID, map);
				ReadFootholds(map);
				ReadLife(map);
				ReadPortals(map);
				ReadSeats(map);
			}
		}

		private static void ReadSeats(Map map) {
			byte Count = Reader.ReadByte();
			for (byte i = 0; i < Count; ++i) {
				Seat seat = new Seat();
				seat.ID = Reader.ReadByte();
				seat.X = Reader.ReadInt16();
				seat.Y = Reader.ReadInt16();
				map.AddSeat(seat);
			}
		}

		private static void ReadPortals(Map map) {
			byte Count = Reader.ReadByte();
			for (byte i = 0; i < Count; ++i) {
				Portal pt = new Portal();
				pt.ID = Reader.ReadByte();
				pt.Name = ReadString();
				pt.ToMapID = Reader.ReadInt32();
				pt.ToName = ReadString();
				pt.X = Reader.ReadInt16();
				pt.Y = Reader.ReadInt16();
				map.AddPortal(pt);
			}
		}

		private static void ReadLife(Map map) {
			ushort Count = Reader.ReadUInt16();
			for (ushort i = 0; i < Count; ++i) {
				Life lf = new Life();
				lf.ID = Reader.ReadInt32();
				lf.X = Reader.ReadInt16();
				lf.Y = Reader.ReadInt16();
				lf.Foothold = Reader.ReadUInt16();
				lf.Cy = Reader.ReadInt16();
				lf.Rx0 = Reader.ReadInt16();
				lf.Rx1 = Reader.ReadInt16();
				lf.RespawnTime = Reader.ReadInt32();
				lf.FacesLeft = Reader.ReadBoolean();
				lf.Type = ReadString();
				map.AddLife(lf);
			}
		}

		private static string ReadString() {
			ushort len = Reader.ReadByte();
			if (len == 0xff)
				return string.Empty;
			if (len == 0)
				len = Reader.ReadUInt16();
			return System.Text.Encoding.ASCII.GetString(Reader.ReadBytes(len));
		}

		private static void ReadFootholds(Map map) {
			ushort Count = Reader.ReadUInt16();
			for (ushort i = 0; i < Count; ++i) {
				Foothold fh = new Foothold();
				fh.ID = Reader.ReadUInt16();
				//fh.NextIdentifier = Reader.ReadUInt16();
				//fh.PreviousIdentifier = Reader.ReadUInt16();
				Reader.ReadBytes(4);
				fh.X1 = Reader.ReadInt16();
				fh.X2 = Reader.ReadInt16();
				fh.Y1 = Reader.ReadInt16();
				fh.Y2 = Reader.ReadInt16();
				map.AddFoothold(fh);
			}
		}
		#endregion
	}

	public class EquipData {
		public int ID { get; set; }
		public bool isCash { get; set; }
		public string Type { get; set; }
		public byte HealHP { get; set; }
		public byte Scrolls { get; set; }
		public byte RequiredLevel { get; set; }
		public ushort RequiredStrength { get; set; }
		public ushort RequiredDexterity { get; set; }
		public ushort RequiredIntellect { get; set; }
		public ushort RequiredLuck { get; set; }
		public ushort RequiredJob { get; set; }
		public int Price { get; set; }
		public byte RequiredFame { get; set; }
		public short HP { get; set; }
		public short MP { get; set; }
		public short Strength { get; set; }
		public short Dexterity { get; set; }
		public short Intellect { get; set; }
		public short Luck { get; set; }
		public byte Hands { get; set; }
		public byte WeaponAttack { get; set; }
		public byte MagicAttack { get; set; }
		public byte WeaponDefense { get; set; }
		public byte MagicDefense { get; set; }
		public byte Accuracy { get; set; }
		public byte Avoidance { get; set; }
		public byte Speed { get; set; }
		public byte Jump { get; set; }
	}

	#region NPCpublic classes
	public class ShopItemData {
		public int ID { get; set; }
		public int Stock { get; set; }
		public int Price { get; set; }
	}

	public class NPCData {
		public int ID { get; set; }
		public string Quest { get; set; }
		public int Trunk { get; set; }
		public List<ShopItemData> Shop { get; set; }
	}
	#endregion

	public class ItemData {
		public int ID { get; set; }
		public int Price { get; set; }
		public bool Cash { get; set; }
		public ushort MaxSlot { get; set; }
		public short HP { get; set; }
		public short MP { get; set; }
		public short HPRate { get; set; }
		public short MPRate { get; set; }
		public short WeaponAttack { get; set; }
		public short MagicAttack { get; set; }
		public short Accuracy { get; set; }
		public short Avoidance { get; set; }
		public short Speed { get; set; }
		public int BuffTime { get; set; }

		public byte CureFlags { get; set; }

		public int MoveTo { get; set; }
		public int Mesos { get; set; }

		public byte ScrollSuccessRate { get; set; }
		public byte ScrollCurseRate { get; set; }
		public byte IncStr { get; set; }
		public byte IncDex { get; set; }
		public byte IncInt { get; set; }
		public byte IncLuk { get; set; }
		public byte IncMHP { get; set; }
		public byte IncMMP { get; set; }
		public byte IncWAtk { get; set; }
		public byte IncMAtk { get; set; }
		public byte IncWDef { get; set; }
		public byte IncMDef { get; set; }
		public byte IncAcc { get; set; }
		public byte IncAvo { get; set; }
		public byte IncJump { get; set; }
		public byte IncSpeed { get; set; }

		public List<ItemSummonInfo> Summons { get; set; }
	}

	public class ItemSummonInfo {
		public int MobID { get; set; }
		public byte Chance { get; set; }
	}

	public class MobSkillLevelData {
		public byte SkillID { get; set; }
		public byte Level { get; set; }
		public short Time { get; set; }
		public short MPConsume { get; set; }
		public int X { get; set; }
		public int Y { get; set; }
		public byte Prop { get; set; }
		public short Cooldown { get; set; }

		public short LTX { get; set; }
		public short LTY { get; set; }
		public short RBX { get; set; }
		public short RBY { get; set; }

		public byte HPLimit { get; set; }
		public byte SummonLimit { get; set; }
		public byte SummonEffect { get; set; }
		public List<int> Summons { get; set; }
	}

	public class MobSkillData {
		public byte Level { get; set; }
		public byte SkillID { get; set; }
		public short EffectAfter { get; set; }
	}

	public class MobAttackData {
		public byte ID { get; set; }
		public short MPConsume { get; set; }
		public short MPBurn { get; set; }
		public short SkillID { get; set; }
		public byte SkillLevel { get; set; }
	}

	public class MobData {
		public int ID { get; set; }
		public byte Level { get; set; }
		public bool Boss { get; set; }
		public bool Undead { get; set; }
		public bool BodyAttack { get; set; }
		public int EXP { get; set; }
		public int MaxHP { get; set; }
		public int MaxMP { get; set; }
		public int HPRecoverAmount { get; set; }
		public int MPRecoverAmount { get; set; }
		public short Speed { get; set; }
		public byte SummonType { get; set; }
		public List<int> Revive { get; set; }
		public Dictionary<byte, MobAttackData> Attacks { get; set; }
		public List<MobSkillData> Skills { get; set; }
	}

	public class SkillLevelData {
		public byte MobCount { get; set; }
		public byte HitCount { get; set; }

		public int BuffTime { get; set; }
		public short Damage { get; set; }
		public short AttackRange { get; set; }
		public byte Mastery { get; set; }

		public short HPProperty { get; set; }
		public short MPProperty { get; set; }
		public short Property { get; set; }

		public short HPUsage { get; set; }
		public short MPUsage { get; set; }
		public int ItemIDUsage { get; set; }
		public short ItemAmountUsage { get; set; }
		public short BulletUsage { get; set; }
		public short MesosUsage { get; set; }

		public short XValue { get; set; }
		public short YValue { get; set; }

		public short Speed { get; set; }
		public short Jump { get; set; }
		public short WeaponAttack { get; set; }
		public short MagicAttack { get; set; }
		public short WeaponDefence { get; set; }
		public short MagicDefence { get; set; }
		public short Accurancy { get; set; }
		public short Avoidability { get; set; }

		public short LTX { get; set; }
		public short LTY { get; set; }
		public short RBX { get; set; }
		public short RBY { get; set; }
	}

	public class DropData {
		public int ItemID { get; set; }
		public int Mesos { get; set; }
        public short Min { get; set; }
		public short Max { get; set; }
		public int Chance { get; set; }
	}



	public class PetData {
		public int ItemID { get; set; }
		public byte Hungry { get; set; }
		public byte Life { get; set; }
		public Dictionary<byte, PetReactionData> Reactions { get; set; }
	}

	public class PetReactionData {
		public byte ReactionID { get; set; }
		public byte Inc { get; set; }
		public byte Prob { get; set; }
	}
}
