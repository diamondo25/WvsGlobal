using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WvsBeta.Common.Sessions;
using WvsBeta.Game;

namespace Connection
{
    public class GameConstants
    {

        public static void GiveStats(Item item, bool DoRandom)
        {
            if (!DataProvider.Equips.ContainsKey(item.ItemID)) return;
            EquipData data = DataProvider.Equips[item.ItemID];
            item.Str = (short)(DoRandom ? GetRandomStat(2, data.Strength) : data.Strength);
            item.Dex = (short)(DoRandom ? GetRandomStat(2, data.Dexterity) : data.Dexterity);
            item.Int = (short)(DoRandom ? GetRandomStat(2, data.Intellect) : data.Intellect);
            item.Luk = (short)(DoRandom ? GetRandomStat(2, data.Luck) : data.Luck);
            item.HP = (short)(DoRandom ? GetRandomStat(10, data.HP) : data.HP);
            item.MP = (short)(DoRandom ? GetRandomStat(10, data.MP) : data.MP);
            item.Watk = (short)(DoRandom ? GetRandomStat(10, data.WeaponAttack) : data.WeaponAttack);
            item.Wdef = (short)(DoRandom ? GetRandomStat(10, data.WeaponDefense) : data.WeaponDefense);
            item.Matk = (short)(DoRandom ? GetRandomStat(10, data.MagicAttack) : data.MagicAttack);
            item.Mdef = (short)(DoRandom ? GetRandomStat(10, data.MagicDefense) : data.MagicDefense);
            item.Acc = (short)(DoRandom ? GetRandomStat(2, data.Accuracy) : data.Accuracy);
            item.Avo = (short)(DoRandom ? GetRandomStat(2, data.Avoidance) : data.Avoidance);
            item.Hands = (short)(DoRandom ? GetRandomStat(4, data.Hands) : data.Hands);
            item.Speed = (short)(DoRandom ? GetRandomStat(4, data.Speed) : data.Speed);
            item.Jump = (short)(DoRandom ? GetRandomStat(2, data.Jump) : data.Jump);
        }
        public static short GetRandomStat(ushort variance, short equipAmount)
        {
            return (short)(equipAmount > 0 ? equipAmount + GetStatVariance(variance) : 0);
        }

        public static short GetStatVariance(ushort amount)
        {
            short s = (short)Server.Instance.Randomizer.ValueBetween(0, amount);
            s -= (short)(amount / 2);
            return s;
        }

        public static short GetStatVarianceScroll(ushort amount)
        {
            return (short)(Server.Instance.Randomizer.ValueBetween(0, 5) * amount);
        }
    }
}

namespace WvsBeta.Game {
	public class Item {
		public int ItemID { get; set; }
		public short InventorySlot { get; set; }
		public byte Slots { get; set; }
		public byte Scrolls { get; set; }
		public short Str { get; set; }
		public short Dex { get; set; }
		public short Int { get; set; }
		public short Luk { get; set; }
		public short HP { get; set; }
		public short MP { get; set; }
		public short Watk { get; set; }
		public short Matk { get; set; }
		public short Wdef { get; set; }
		public short Mdef { get; set; }
		public short Acc { get; set; }
		public short Avo { get; set; }
		public short Hands { get; set; }
		public short Jump { get; set; }
		public short Speed { get; set; }
		public short Amount { get; set; }
		public long CashId { get; set; }
		public long Expiration { get; set; }
		public string Name { get; set; }
		public Pet Pet { get; set; }

		public Item() {
			ItemID = 0;
			Amount = 0;
			Slots = 7;
			Scrolls = 0;
			Str = 0;
			Dex = 0;
			Int = 0;
			Luk = 0;
			HP = 0;
			MP = 0;
			Watk = 0;
			Matk = 0;
			Wdef = 0;
			Mdef = 0;
			Acc = 0;
			Avo = 0;
			Hands = 0;
			Jump = 0;
			Speed = 0;
			CashId = 0;
			Expiration = 150842304000000000L;
			Name = "";
			Pet = null;
		}

		public Item(Item itemBase) {
			ItemID = itemBase.ItemID;
			Amount = itemBase.Amount;
			Slots = itemBase.Slots;
			Scrolls = itemBase.Scrolls;
			Str = itemBase.Str;
			Dex = itemBase.Dex;
			Int = itemBase.Int;
			Luk = itemBase.Luk;
			HP = itemBase.HP;
			MP = itemBase.MP;
			Watk = itemBase.Watk;
			Matk = itemBase.Matk;
			Wdef = itemBase.Wdef;
			Mdef = itemBase.Mdef;
			Acc = itemBase.Acc;
			Avo = itemBase.Avo;
			Hands = itemBase.Hands;
			Jump = itemBase.Jump;
			Speed = itemBase.Speed;
			CashId = itemBase.CashId;
			Expiration = itemBase.Expiration;
			Name = itemBase.Name;
			Pet = itemBase.Pet;
		}

		public void GiveStats(bool DoRandom) {
			if (!DataProvider.Equips.ContainsKey(ItemID)) return;
			EquipData data = DataProvider.Equips[ItemID];
			Str = (short)(DoRandom ? GetRandomStat(2, data.Strength) : data.Strength);
			Dex = (short)(DoRandom ? GetRandomStat(2, data.Dexterity) : data.Dexterity);
			Int = (short)(DoRandom ? GetRandomStat(2, data.Intellect) : data.Intellect);
			Luk = (short)(DoRandom ? GetRandomStat(2, data.Luck) : data.Luck);
			HP = (short)(DoRandom ? GetRandomStat(10, data.HP) : data.HP);
			MP = (short)(DoRandom ? GetRandomStat(10, data.MP) : data.MP);
			Watk = (short)(DoRandom ? GetRandomStat(10, data.WeaponAttack) : data.WeaponAttack);
			Wdef = (short)(DoRandom ? GetRandomStat(10, data.WeaponDefense) : data.WeaponDefense);
			Matk = (short)(DoRandom ? GetRandomStat(10, data.MagicAttack) : data.MagicAttack);
			Mdef = (short)(DoRandom ? GetRandomStat(10, data.MagicDefense) : data.MagicDefense);
			Acc = (short)(DoRandom ? GetRandomStat(2, data.Accuracy) : data.Accuracy);
			Avo = (short)(DoRandom ? GetRandomStat(2, data.Avoidance) : data.Avoidance);
			Hands = (short)(DoRandom ? GetRandomStat(4, data.Hands) : data.Hands);
			Speed = (short)(DoRandom ? GetRandomStat(4, data.Speed) : data.Speed);
			Jump = (short)(DoRandom ? GetRandomStat(2, data.Jump) : data.Jump);
		}

		private short GetRandomStat(ushort variance, short equipAmount) {
			return (short)(equipAmount > 0 ? equipAmount + GetStatVariance(variance) : 0);
		}

		private short GetStatVariance(ushort amount) {
			Random rnd = new Random();
			short s = (short)rnd.Next(amount);
			s -= (short)(amount / 2);
			return s;
		}
	}
}
