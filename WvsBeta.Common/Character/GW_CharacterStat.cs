using MySql.Data.MySqlClient;
using WvsBeta.Common.Sessions;
using WvsBeta.Database;

namespace WvsBeta.Common.Character
{
    public class GW_CharacterStat
    {
        public int ID { get; protected set; }
        public string Name { get; protected set; }

        public byte Gender { get; protected set; }
        public byte Skin { get; protected set; }
        public int Face { get; protected set; }
        public int Hair { get; protected set; }

        public long PetCashId { get; protected set; }

        public byte Level { get; protected set; }
        public short Job { get; protected set; }
        public short Str { get; protected set; }
        public short Dex { get; protected set; }
        public short Int { get; protected set; }
        public short Luk { get; protected set; }
        public short HP { get; protected set; }
        public short MaxHP { get; protected set; }
        public short MP { get; protected set; }
        public short MaxMP { get; protected set; }
        public short AP { get; protected set; }
        public short SP { get; protected set; }
        public int EXP { get; protected set; }
        public short Fame { get; protected set; }

        public int MapID { get; protected set; }
        public byte MapPosition { get; protected set; }

        public int Money { get; protected set; }
        
        public void LoadFromReader(MySqlDataReader data)
        {
            ID = data.GetInt32("id");
            Name = data.GetString("name");
            Gender = data.GetByte("gender");
            Skin = data.GetByte("skin");
            Hair = data.GetInt32("hair");
            Face = data.GetInt32("eyes");

            MapID = data.GetInt32("map");
            MapPosition = (byte)data.GetInt16("pos");

            Level = data.GetByte("level");
            Job = data.GetInt16("job");
            Str = data.GetInt16("str");
            Dex = data.GetInt16("dex");
            Int = data.GetInt16("int");
            Luk = data.GetInt16("luk");
            HP = data.GetInt16("chp");
            MaxHP = data.GetInt16("mhp");
            MP = data.GetInt16("cmp");
            MaxMP = data.GetInt16("mmp");
            AP = data.GetInt16("ap");
            SP = data.GetInt16("sp");
            EXP = data.GetInt32("exp");
            Fame = data.GetInt16("fame");

            Money = data.GetInt32("mesos");
        }

        public void Encode(Packet pPacket)
        {
            pPacket.WriteInt(ID);
            pPacket.WriteString(Name, 13);


            pPacket.WriteByte(Gender); // Gender
            pPacket.WriteByte(Skin); // Skin
            pPacket.WriteInt(Face); // Face
            pPacket.WriteInt(Hair); // Hair

            pPacket.WriteLong(PetCashId);

            pPacket.WriteByte(Level);
            pPacket.WriteShort(Job);
            pPacket.WriteShort(Str);
            pPacket.WriteShort(Dex);
            pPacket.WriteShort(Int);
            pPacket.WriteShort(Luk);
            pPacket.WriteShort(HP);
            pPacket.WriteShort(MaxHP);
            pPacket.WriteShort(MP);
            pPacket.WriteShort(MaxMP);
            pPacket.WriteShort(AP);
            pPacket.WriteShort(SP);
            pPacket.WriteInt(EXP);
            pPacket.WriteShort(Fame);

            pPacket.WriteInt(MapID);
            pPacket.WriteByte(MapPosition);


            pPacket.WriteLong(0); // I have still no idea what these are
            pPacket.WriteInt(0);
            pPacket.WriteInt(0);
        }

        public void EncodeMoney(Packet pPacket)
        {
            pPacket.WriteInt(Money); // Hell yea
        }

        public void Decode(Packet pPacket)
        {
            ID = pPacket.ReadInt();
            Name = pPacket.ReadString(13);


            Gender = pPacket.ReadByte();
            Skin = pPacket.ReadByte();
            Face = pPacket.ReadInt();
            Hair = pPacket.ReadInt();

            PetCashId = pPacket.ReadLong();

            Level = pPacket.ReadByte();
            Job = pPacket.ReadShort();
            Str = pPacket.ReadShort();
            Dex = pPacket.ReadShort();
            Int = pPacket.ReadShort();
            Luk = pPacket.ReadShort();
            HP = pPacket.ReadShort();
            MaxHP = pPacket.ReadShort();
            MP = pPacket.ReadShort();
            MaxMP = pPacket.ReadShort();
            AP = pPacket.ReadShort();
            SP = pPacket.ReadShort();
            EXP = pPacket.ReadInt();
            Fame = pPacket.ReadShort();

            MapID = pPacket.ReadInt();
            MapPosition = pPacket.ReadByte();


            pPacket.ReadLong(); // I have still no idea what these are
            pPacket.ReadInt();
            pPacket.ReadInt();
        }

        public void DecodeMoney(Packet pPacket)
        {
            Money = pPacket.ReadInt();
        }
    }
}
