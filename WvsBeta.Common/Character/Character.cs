using WvsBeta.Common.Character;
using WvsBeta.Common.Sessions;

namespace WvsBeta.Common
{
    public class CharacterBase : MovableLife
    {
        public string Name { get; set; }
        public int ID { get; set; }
        public short Job { get; set; } // Center is the only server that uses this for jobs. Game uses PrimaryStats for job.
        public byte Level { get; set; } // Center is the only server that uses this for level. Game uses PrimaryStats for level.

        public byte Gender { get; set; }
        public byte Skin { get; set; }
        public int Face { get; set; }
        public int Hair { get; set; }

        public virtual int MapID { get; set; }

        public virtual int PartyID { get; set; }

        public bool IsOnline { get; set; }

        public byte GMLevel { get; set; }
        public bool IsGM { get => GMLevel > 0; }
        public bool IsAdmin { get => GMLevel >= 3; }
        
        public void EncodeForTransfer(Packet pw)
        {
            pw.WriteString(Name);
            pw.WriteInt(ID);
            pw.WriteShort(Job);
            pw.WriteByte(Level);

            pw.WriteByte(Gender);
            pw.WriteByte(Skin);
            pw.WriteInt(Face);
            pw.WriteInt(Hair);

            pw.WriteInt(MapID);
            pw.WriteInt(PartyID);
            pw.WriteBool(IsOnline);
            pw.WriteByte(GMLevel);
        }


        public void DecodeForTransfer(Packet pr)
        {
            Name = pr.ReadString();
            ID = pr.ReadInt();
            Job = pr.ReadShort();
            Level = pr.ReadByte();

            Gender = pr.ReadByte();
            Skin = pr.ReadByte();
            Face = pr.ReadInt();
            Hair = pr.ReadInt();

            MapID = pr.ReadInt();
            PartyID = pr.ReadInt();
            IsOnline = pr.ReadBool();
            GMLevel = pr.ReadByte();
        }
    }
}
