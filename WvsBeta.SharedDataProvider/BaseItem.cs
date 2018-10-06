using System;
using System.Runtime.CompilerServices;
using MySql.Data.MySqlClient;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;

namespace WvsBeta.Game
{
    public abstract class BaseItem
    {
        public int ItemID { get; set; } = 0;
        public short Amount { get; set; }
        public short InventorySlot { get; set; } = 0;
        public long CashId { get; set; }
        public long Expiration { get; set; } = NoItemExpiration;
        public bool AlreadyInDatabase { get; set; } = false;

        public const long NoItemExpiration = 150842304000000000L;

        protected BaseItem()
        {
        }

        protected BaseItem(BaseItem itemBase)
        {
            ItemID = itemBase.ItemID;
            Amount = itemBase.Amount;
            CashId = itemBase.CashId;
            Expiration = itemBase.Expiration;
        }

        public BaseItem Duplicate()
        {
            if (this is EquipItem ei) return new EquipItem(ei);
            if (this is PetItem pi) return new PetItem(pi);
            if (this is BundleItem bi) return new BundleItem(bi);
            return null;
        }

        public BaseItem SplitInTwo(short secondPairAmount)
        {
            if (this.Amount < secondPairAmount) return null;

            if (CashId != 0) throw new Exception("Trying to split a cashitem in two!!!");

            var dupe = Duplicate();
            this.Amount -= secondPairAmount;

            dupe.Amount = secondPairAmount;
            return dupe;
        }

        public static BaseItem CreateFromItemID(int itemId, short amount = 1)
        {
            if (itemId == 0) throw new Exception("Invalid ItemID in CreateFromItemID");

            int itemType = (itemId / 1000000);

            BaseItem ret;
            if (itemType == 1) ret = new EquipItem();
            else if (itemType == 5) ret = new PetItem(); // TODO: Pet
            else ret = new BundleItem();

            ret.ItemID = itemId;
            ret.Amount = amount;
            return ret;
        }

        public virtual void GiveStats(ItemVariation enOption)
        {
        }

        public virtual void Load(MySqlDataReader data)
        {
            // Load ItemID manually

            if (ItemID == 0) throw new Exception("Tried to Load() an item while CreateFromItemID was not used.");

            AlreadyInDatabase = true;
            CashId = data.GetInt64("cashid");
            Expiration = data.GetInt64("expiration");
        }

        public void EncodeForMigration(Packet pw)
        {
            pw.WriteInt(ItemID);

            pw.WriteShort(Amount);

            if (this is EquipItem equipItem)
            {
                pw.WriteByte(equipItem.Slots);
                pw.WriteByte(equipItem.Scrolls);
                pw.WriteShort(equipItem.Str);
                pw.WriteShort(equipItem.Dex);
                pw.WriteShort(equipItem.Int);
                pw.WriteShort(equipItem.Luk);
                pw.WriteShort(equipItem.HP);
                pw.WriteShort(equipItem.MP);
                pw.WriteShort(equipItem.Watk);
                pw.WriteShort(equipItem.Matk);
                pw.WriteShort(equipItem.Wdef);
                pw.WriteShort(equipItem.Mdef);
                pw.WriteShort(equipItem.Acc);
                pw.WriteShort(equipItem.Avo);
                pw.WriteShort(equipItem.Hands);
                pw.WriteShort(equipItem.Jump);
                pw.WriteShort(equipItem.Speed);
            }
            else
            {
                pw.WriteByte(0);
                pw.WriteByte(0);
                pw.WriteShort(0);
                pw.WriteShort(0);
                pw.WriteShort(0);
                pw.WriteShort(0);
                pw.WriteShort(0);
                pw.WriteShort(0);
                pw.WriteShort(0);
                pw.WriteShort(0);
                pw.WriteShort(0);
                pw.WriteShort(0);
                pw.WriteShort(0);
                pw.WriteShort(0);
                pw.WriteShort(0);
                pw.WriteShort(0);
                pw.WriteShort(0);
            }

            pw.WriteLong(CashId);
            pw.WriteLong(Expiration);

            pw.WriteString("");
        }

        public static BaseItem DecodeForMigration(Packet pr)
        {
            var itemId = pr.ReadInt();

            var item = CreateFromItemID(itemId);
            item.ItemID = itemId;

            item.Amount = pr.ReadShort();

            if (item is EquipItem equipItem)
            {
                equipItem.Slots = pr.ReadByte();
                equipItem.Scrolls = pr.ReadByte();
                equipItem.Str = pr.ReadShort();
                equipItem.Dex = pr.ReadShort();
                equipItem.Int = pr.ReadShort();
                equipItem.Luk = pr.ReadShort();
                equipItem.HP = pr.ReadShort();
                equipItem.MP = pr.ReadShort();
                equipItem.Watk = pr.ReadShort();
                equipItem.Matk = pr.ReadShort();
                equipItem.Wdef = pr.ReadShort();
                equipItem.Mdef = pr.ReadShort();
                equipItem.Acc = pr.ReadShort();
                equipItem.Avo = pr.ReadShort();
                equipItem.Hands = pr.ReadShort();
                equipItem.Jump = pr.ReadShort();
                equipItem.Speed = pr.ReadShort();
            }
            else
            {
                pr.ReadByte();
                pr.ReadByte();
                pr.ReadShort();
                pr.ReadShort();
                pr.ReadShort();
                pr.ReadShort();
                pr.ReadShort();
                pr.ReadShort();
                pr.ReadShort();
                pr.ReadShort();
                pr.ReadShort();
                pr.ReadShort();
                pr.ReadShort();
                pr.ReadShort();
                pr.ReadShort();
                pr.ReadShort();
                pr.ReadShort();
            }

            item.CashId = pr.ReadLong();
            item.Expiration = pr.ReadLong();

            pr.ReadString();

            return item;
        }

        public virtual void Encode(Packet packet)
        {
            packet.WriteInt(ItemID);

            packet.WriteBool(CashId != 0);
            if (CashId != 0)
                packet.WriteLong(CashId);

            packet.WriteLong(Expiration);

        }

        /// <summary>
        /// Build a full insert statement that is not optimized.
        /// </summary>
        /// <returns>A comma delimited set of fields</returns>
        public virtual string GetFullSaveColumns()
        {
            throw new NotImplementedException();
        }

        public virtual string GetFullUpdateColumns()
        {
            throw new NotImplementedException();
        }
    }

    public class BundleItem : BaseItem
    {
        public BundleItem() { }

        public BundleItem(BundleItem itemBase) : base(itemBase) { }

        public override void Load(MySqlDataReader data)
        {
            base.Load(data);
            Amount = data.GetInt16("amount");
        }


        public override void Encode(Packet packet)
        {
            base.Encode(packet);
            packet.WriteShort(Amount);
        }

        public override string GetFullSaveColumns()
        {
            return
                ItemID + ", " +
                Amount + ", " +
                CashId + ", " +
                Expiration;
        }

        public override string GetFullUpdateColumns()
        {
            return
                "itemid = " + ItemID + ", " +
                "amount = " + Amount + ", " +
                "cashid = " + CashId + ", " +
                "expiration = " + Expiration;
        }
    }

    public enum ItemVariation
    {
        None = 0,
        Better = 1,
        Normal = 2,
        Great = 3,
        Gachapon = 4,
    }

    public class EquipItem : BaseItem
    {
        public byte Slots { get; set; } = 7;
        public byte Scrolls { get; set; } = 0;
        public short Str { get; set; } = 0;
        public short Dex { get; set; } = 0;
        public short Int { get; set; } = 0;
        public short Luk { get; set; } = 0;
        public short HP { get; set; } = 0;
        public short MP { get; set; } = 0;
        public short Watk { get; set; } = 0;
        public short Matk { get; set; } = 0;
        public short Wdef { get; set; } = 0;
        public short Mdef { get; set; } = 0;
        public short Acc { get; set; } = 0;
        public short Avo { get; set; } = 0;
        public short Hands { get; set; } = 0;
        public short Jump { get; set; } = 0;
        public short Speed { get; set; } = 0;

        public static EquipItem DummyEquipItem { get; } = new EquipItem();

        public EquipItem() { }

        public EquipItem(EquipItem itemBase) : base(itemBase)
        {
            Amount = 1;
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
        }

        public override void GiveStats(ItemVariation enOption)
        {
            if (!BaseDataProvider.Equips.TryGetValue(ItemID, out EquipData data))
            {
                return;
            }

            Slots = data.Slots;
            Amount = 1; // Force it to be 1.

            if (enOption != ItemVariation.None)
            {
                Str = GetVariation(data.Strength, enOption);
                Dex = GetVariation(data.Dexterity, enOption);
                Int = GetVariation(data.Intellect, enOption);
                Luk = GetVariation(data.Luck, enOption);
                HP = GetVariation(data.HP, enOption);
                MP = GetVariation(data.MP, enOption);
                Watk = GetVariation(data.WeaponAttack, enOption);
                Wdef = GetVariation(data.WeaponDefense, enOption);
                Matk = GetVariation(data.MagicAttack, enOption);
                Mdef = GetVariation(data.MagicDefense, enOption);
                Acc = GetVariation(data.Accuracy, enOption);
                Avo = GetVariation(data.Avoidance, enOption);
                Hands = GetVariation(data.Hands, enOption);
                Speed = GetVariation(data.Speed, enOption);
                Jump = GetVariation(data.Jump, enOption);
            }
            else
            {
                Str = data.Strength;
                Dex = data.Dexterity;
                Int = data.Intellect;
                Luk = data.Luck;
                HP = data.HP;
                MP = data.MP;
                Watk = data.WeaponAttack;
                Wdef = data.WeaponDefense;
                Matk = data.MagicAttack;
                Mdef = data.MagicDefense;
                Acc = data.Accuracy;
                Avo = data.Avoidance;
                Hands = data.Hands;
                Speed = data.Speed;
                Jump = data.Jump;
            }

        }


        public override void Load(MySqlDataReader data)
        {
            base.Load(data);

            Slots = (byte)data.GetInt16("slots");
            Scrolls = (byte)data.GetInt16("scrolls");
            Str = data.GetInt16("istr");
            Dex = data.GetInt16("idex");
            Int = data.GetInt16("iint");
            Luk = data.GetInt16("iluk");
            HP = data.GetInt16("ihp");
            MP = data.GetInt16("imp");
            Watk = data.GetInt16("iwatk");
            Matk = data.GetInt16("imatk");
            Wdef = data.GetInt16("iwdef");
            Mdef = data.GetInt16("imdef");
            Acc = data.GetInt16("iacc");
            Avo = data.GetInt16("iavo");
            Hands = data.GetInt16("ihand");
            Speed = data.GetInt16("ispeed");
            Jump = data.GetInt16("ijump");
        }

        public override void Encode(Packet packet)
        {
            base.Encode(packet);

            packet.WriteByte(Slots);
            packet.WriteByte(Scrolls);
            packet.WriteShort(Str);
            packet.WriteShort(Dex);
            packet.WriteShort(Int);
            packet.WriteShort(Luk);
            packet.WriteShort(HP);
            packet.WriteShort(MP);
            packet.WriteShort(Watk);
            packet.WriteShort(Matk);
            packet.WriteShort(Wdef);
            packet.WriteShort(Mdef);
            packet.WriteShort(Acc);
            packet.WriteShort(Avo);
            packet.WriteShort(Hands);
            packet.WriteShort(Speed);
            packet.WriteShort(Jump);
        }

        public override string GetFullSaveColumns()
        { 
            return (
                ItemID + ", " +
                Slots + ", " +
                Scrolls + ", " +
                Str + ", " +
                Dex + ", " +
                Int + ", " +
                Luk + ", " +
                HP + ", " +
                MP + ", " +
                Watk + ", " +
                Matk + ", " +
                Wdef + ", " +
                Mdef + ", " +
                Acc + ", " +
                Avo + ", " +
                Hands + ", " +
                Speed + ", " +
                Jump + ", " +
                CashId + ", " +
                Expiration
            );
        }

        public override string GetFullUpdateColumns()
        {
            return (
                "itemid = " + ItemID + ", " +
                "slots = " + Slots + ", " +
                "scrolls = " + Scrolls + ", " +
                "istr = " + Str + ", " +
                "idex = " + Dex + ", " +
                "iint = " + Int + ", " +
                "iluk = " + Luk + ", " +
                "ihp = " + HP + ", " +
                "imp = " + MP + ", " +
                "iwatk = " + Watk + ", " +
                "imatk = " + Matk + ", " +
                "iwdef = " + Wdef + ", " +
                "imdef = " + Mdef + ", " +
                "iacc = " + Acc + ", " +
                "iavo = " + Avo + ", " +
                "ihand = " + Hands + ", " +
                "ispeed = " + Speed + ", " +
                "ijump = " + Jump + ", " +
                "cashid = " + CashId + ", " +
                "expiration = " + Expiration
            );
        }

        public static short GetVariation(short v, ItemVariation enOption)
        {
            if (v <= 0) return 0;
            if (enOption == ItemVariation.Gachapon)
            {
                // TODO: Gacha
                return v;
            }
            // This logic has 2 bonus bits.

            int maxDiff = Math.Min(v / 10 + 1, 5); // Max stat

            // Maximum amount of bits to set
            // Note:
            // Default: 1 << (1 + 2) == 0x08 (3 bits)
            // Max:     1 << (5 + 2) == 0x80 (7 bits)
            uint maxBits = (uint)(1 << (maxDiff + 2));
            int randBits = (int)(Rand32.Next() % maxBits);

            // Trace.WriteLine($"{(v11 >> 6) & 1} {(v11 >> 5) & 1} | {(v11 >> 4) & 1} {(v11 >> 3) & 1} {(v11 >> 2) & 1} {(v11 >> 1) & 1} {(v11 >> 0) & 1} ");

            // 0 - 3 range
            int calculatedBoost =
                0
                + ((randBits >> 4) & 1)
                + ((randBits >> 3) & 1)
                + ((randBits >> 2) & 1)
                + ((randBits >> 1) & 1)
                + ((randBits >> 0) & 1)
                // Additional bonus
                - 2
                + ((randBits >> 5) & 1)
                + ((randBits >> 6) & 1);

            // Trace.WriteLine($"Boost w/ bonus: {calculatedBoost}");

            // Make sure we don't give negative boost
            calculatedBoost = Math.Max(0, calculatedBoost);

            //Trace.WriteLine($"Actual boost: {calculatedBoost}");


            // Normal is the only one that can go down. The rest goes up
            if (enOption == ItemVariation.Normal)
            {
                if ((Rand32.Next() & 1) == 0)
                    return (short)(v - calculatedBoost);
                else
                    return (short)(v + calculatedBoost);
            }
            else if (enOption == ItemVariation.Better)
            {
                if ((Rand32.Next() % 10) < 3)
                    return v;
                else
                    return (short)(v + calculatedBoost);
            }
            else if (enOption == ItemVariation.Great)
            {
                if ((Rand32.Next() % 10) < 1)
                    return v;
                else
                    return (short)(v + calculatedBoost);
            }
            else
            {
                throw new Exception("Invalid ItemVariation");
            }
        }

    }

    public class PetItem : BaseItem
    {
        public string Name { get; set; }
        public byte Level { get; set; }
        public short Closeness { get; set; }
        public byte Fullness { get; set; }
        public long DeadDate { get; set; }

        public MovableLife MovableLife { get; } = new MovableLife();

        public static PetItem DummyPetItem { get; } = new PetItem();

        public PetItem(PetItem itemBase) : base(itemBase)
        {
            Name = itemBase.Name;
            Level = itemBase.Level;
            Closeness = itemBase.Closeness;
            Fullness = itemBase.Fullness;
            DeadDate = itemBase.DeadDate;
        }

        public PetItem() { }


        public override void Load(MySqlDataReader data)
        {
            base.Load(data);

            Name = data.GetString("name");
            Level = data.GetByte("level");
            Closeness = data.GetInt16("closeness");
            Fullness = data.GetByte("fullness");
            DeadDate = data.GetInt64("deaddate");
        }

        public override void Encode(Packet packet)
        {
            base.Encode(packet);

            packet.WriteString(Name, 13);
            packet.WriteByte(Level);
            packet.WriteShort(Closeness);
            packet.WriteByte(Fullness);
            packet.WriteLong(DeadDate);
        }

        public override string GetFullSaveColumns()
        {
            return
                CashId + "," +
                ItemID + "," +
                "'" + MySqlHelper.EscapeString(Name) + "'," +
                Level + "," +
                Closeness + "," +
                Fullness + "," +
                Expiration + "," +
                DeadDate + "";
        }

        public override string GetFullUpdateColumns()
        {
            return
                "cashid = " + CashId + "," +
                "itemid = " + ItemID + "," +
                "name = '" + MySqlHelper.EscapeString(Name) + "'," +
                "level = " + Level + "," +
                "closeness = " + Closeness + "," +
                "fullness = " + Fullness + "," +
                "expiration = " + Expiration + "," +
                "deaddate = " + DeadDate + "";
        }
    }

    /// <summary>
    /// Backwards compat
    /// </summary>
    [Obsolete(
        "Replaced by EquipItem, PetItem, BundleItem. To create one without the specific type, use BaseItem.CreateFromItemID")]
    public class Item
    {
        public int ItemID { get; set; } = 0;
        public short Amount { get; set; }
        public short InventorySlot { get; set; } = 0;
        public long CashId { get; set; }
        public long Expiration { get; set; } = BaseItem.NoItemExpiration;
        public byte Slots { get; set; } = 7;
        public byte Scrolls { get; set; } = 0;
        public short Str { get; set; } = 0;
        public short Dex { get; set; } = 0;
        public short Int { get; set; } = 0;
        public short Luk { get; set; } = 0;
        public short HP { get; set; } = 0;
        public short MP { get; set; } = 0;
        public short Watk { get; set; } = 0;
        public short Matk { get; set; } = 0;
        public short Wdef { get; set; } = 0;
        public short Mdef { get; set; } = 0;
        public short Acc { get; set; } = 0;
        public short Avo { get; set; } = 0;
        public short Hands { get; set; } = 0;
        public short Jump { get; set; } = 0;
        public short Speed { get; set; } = 0;

        public Item()
        {
        }

        public Item(Item itemBase)
        {
            ItemID = itemBase.ItemID;
            Amount = itemBase.Amount;
            CashId = itemBase.CashId;
            Expiration = itemBase.Expiration;

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
        }


        private EquipItem ToEquipItem()
        {
            return new EquipItem
            {
                Acc = Acc,
                Amount = Amount,
                Avo = Avo,
                CashId = CashId,
                Dex = Dex,
                Expiration = Expiration,
                Hands = Hands,
                HP = HP,
                Int = Int,
                InventorySlot = InventorySlot,
                ItemID = ItemID,
                Slots = Slots,
                MP = MP,
                Speed = Speed,
                Jump = Jump,
                Luk = Luk,
                Matk = Matk,
                Mdef = Mdef,
                Scrolls = Scrolls,
                Str = Str,
                Watk = Watk,
                Wdef = Wdef
            };
        }

        private BundleItem ToBundleItem()
        {
            return new BundleItem
            {
                Amount = Amount,
                CashId = CashId,
                Expiration = Expiration,
                InventorySlot = InventorySlot,
                ItemID = ItemID,
            };
        }

        public static implicit operator EquipItem(Item i) => i.ToEquipItem();

        public static implicit operator BundleItem(Item i) => i.ToBundleItem();

        public static implicit operator BaseItem(Item i)
        {
            var inventory = Constants.getInventory(i.ItemID);
            if (inventory == 1) return i.ToEquipItem();
            if (inventory == 5) return null; // TODO: ???
            return i.ToBundleItem();
        }
    }
}
