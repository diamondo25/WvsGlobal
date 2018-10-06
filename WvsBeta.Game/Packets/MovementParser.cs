using log4net;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;

namespace WvsBeta.Game.Packets
{
    public class MovePath
    {
        public enum MovementType : byte
        {
            NormalMovement = 0,
            Jump = 1,
            JumpKb = 2,
            Immediate = 3, // GM F1 teleport
            Teleport = 4,
            NormalMovement2 = 5,
            FlashJump = 6,
            Assaulter = 7,
            Falling = 8,
        }

        public struct MoveElement
        {
            public MovementType Type;
            public short X, XVelocity;
            public short Y, YVelocity;
            public short Foothold;
            public short TimeElapsed;
            public byte Stance, Stat;
        }

        public enum MovementSource
        {
            Player,
            Mob,
            Pet,
            Summon
        }

        public MoveElement[] Elements { get; private set; }
        public Pos OriginalPosition { get; private set; }
        public Pos NewPosition { get; private set; }
        public byte NewStance { get; private set; }
        public short NewFoothold { get; private set; }
        public MovementSource Source { get; private set; }
        public long PacketCreationDate { get; private set; }

        public void DecodeFromPacket(Packet packet, MovementSource source)
        {
            PacketCreationDate = packet.PacketCreationTime;

            Source = source;
            short foothold = 0;
            byte stance = 0;

            var x = packet.ReadShort();
            var y = packet.ReadShort();

            OriginalPosition = new Pos(x, y);

            var movementCount = packet.ReadByte();

            Elements = new MoveElement[movementCount];

            for (var i = 0; i < movementCount; i++)
            {
                var mt = (MovementType)packet.ReadByte();
                var me = new MoveElement
                {
                    Type = mt,
                    Foothold = foothold,
                    X = x,
                    Y = y,
                    Stance = stance
                };

                switch (mt)
                {
                    case MovementType.NormalMovement:
                    case MovementType.NormalMovement2:
                        me.X = packet.ReadShort();
                        me.Y = packet.ReadShort();
                        me.XVelocity = packet.ReadShort();
                        me.YVelocity = packet.ReadShort();
                        me.Foothold = packet.ReadShort();

                        me.Stance = packet.ReadByte();
                        me.TimeElapsed = packet.ReadShort();
                        break;

                    case MovementType.Jump:
                    case MovementType.JumpKb:
                    case MovementType.FlashJump:
                        me.XVelocity = packet.ReadShort();
                        me.YVelocity = packet.ReadShort();

                        me.Stance = packet.ReadByte();
                        me.TimeElapsed = packet.ReadShort();
                        break;


                    case MovementType.Immediate:
                    case MovementType.Teleport:
                    case MovementType.Assaulter:
                        me.X = packet.ReadShort();
                        me.Y = packet.ReadShort();
                        me.Foothold = packet.ReadShort();

                        me.Stance = packet.ReadByte();
                        me.TimeElapsed = packet.ReadShort();
                        break;

                    case MovementType.Falling:
                        me.Stat = packet.ReadByte();
                        break;

                    default:
                        me.Stance = packet.ReadByte();
                        me.TimeElapsed = packet.ReadShort();
                        break;

                }

                x = me.X;
                y = me.Y;
                foothold = me.Foothold;
                stance = me.Stance;

                Elements[i] = me;
            }

            var keypadStates = packet.ReadByte();
            for (var i = 0; i < keypadStates; i++)
            {
                if ((i % 2) == 0) packet.ReadByte();
            }

            NewPosition = new Pos(x, y);
            NewStance = stance;
            NewFoothold = foothold;
        }

        public void EncodeToPacket(Packet packet)
        {
            packet.WriteShort(OriginalPosition.X);
            packet.WriteShort(OriginalPosition.Y);

            packet.WriteByte((byte)Elements.Length);

            foreach (var me in Elements)
            {
                packet.WriteByte((byte)me.Type);

                switch (me.Type)
                {
                    case MovementType.NormalMovement:
                    case MovementType.NormalMovement2:
                        packet.WriteShort(me.X);
                        packet.WriteShort(me.Y);
                        packet.WriteShort(me.XVelocity);
                        packet.WriteShort(me.YVelocity);
                        packet.WriteShort(me.Foothold);

                        packet.WriteByte(me.Stance);
                        packet.WriteShort(me.TimeElapsed);
                        break;

                    case MovementType.Jump:
                    case MovementType.JumpKb:
                    case MovementType.FlashJump:
                        packet.WriteShort(me.XVelocity);
                        packet.WriteShort(me.YVelocity);

                        packet.WriteByte(me.Stance);
                        packet.WriteShort(me.TimeElapsed);
                        break;


                    case MovementType.Immediate:
                    case MovementType.Teleport:
                    case MovementType.Assaulter:
                        packet.WriteShort(me.X);
                        packet.WriteShort(me.Y);
                        packet.WriteShort(me.Foothold);

                        packet.WriteByte(me.Stance);
                        packet.WriteShort(me.TimeElapsed);
                        break;

                    case MovementType.Falling:
                        packet.WriteByte(me.Stat);
                        break;

                    default:
                        packet.WriteByte(me.Stance);
                        packet.WriteShort(me.TimeElapsed);
                        break;

                }
            }
        }

        private static ILog _log = LogManager.GetLogger("MovePath");

        public void Dump()
        {
            _log.Debug(this);
        }
    }
}
