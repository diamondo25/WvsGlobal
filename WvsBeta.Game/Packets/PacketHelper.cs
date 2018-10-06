using System.Diagnostics;
using System.Linq;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;
using WvsBeta.Game.Packets;
using WvsBeta.SharedDataProvider;

namespace WvsBeta.Game
{
    internal class PacketHelper : BasePacketHelper
    {
        public static bool ValidateMovePath(MovableLife life, MovePath path)
        {
            short Foothold = life.Foothold, X = life.Position.X, Y = life.Position.Y;
            short startX = life.Position.X;
            short startY = life.Position.Y;
            bool toRet = true;
            bool needCheck = true;

            byte amount = (byte)path.Elements.Length;
            for (byte i = 0; i < amount; i++)
            {
                var me = path.Elements[i];
                switch (me.Type)
                {
                    case MovePath.MovementType.NormalMovement:
                    case MovePath.MovementType.NormalMovement2:
                        life.Wobble.X = me.XVelocity;
                        life.Wobble.Y = me.YVelocity;

                        if (me.Stance < 5)
                            life.Jumps = 0;
                        break;

                    case MovePath.MovementType.Jump:
                    case MovePath.MovementType.JumpKb:
                    case MovePath.MovementType.FlashJump: //jump, here we check for jumpingshit
                        if (life.Jumps > 5) toRet = false;

                        life.Jumps++;
                        break;

                    case MovePath.MovementType.Immediate:
                    case MovePath.MovementType.Teleport:
                    case MovePath.MovementType.Assaulter:

                        life.Wobble.X = me.XVelocity;
                        life.Wobble.Y = me.YVelocity;

                        break;
                }
            }

            // TODO: Check foothold IDs for wrong ones (WZ hack)

            life.Foothold = path.NewFoothold;
            life.Position = new Pos(path.NewPosition);
            life.Stance = path.NewStance;
            life.LastMove = MasterThread.CurrentTime;


            if (false)
            {
                // Some testing code for speedhacks.
                // This does seem to work, however. 

                life.MovePathTimeSum += path.Elements.Sum(x => x.TimeElapsed);

                if (life.MovePathTimeSumLastCheck == 0)
                    life.MovePathTimeSumLastCheck = life.LastMove;

                var millisSinceLastCheck = (life.LastMove - life.MovePathTimeSumLastCheck);

                // After a couple seconds, check
                if (millisSinceLastCheck > 1000)
                {
                    if (life.MovePathTimeSum > (millisSinceLastCheck * 1.4))
                    {
                        life.MovePathTimeHackCount++;

                        if (life.MovePathTimeHackCount > 5)
                        {
                            Trace.WriteLine("!!");
                            return false;
                        }
                    }
                    life.MovePathTimeSum = 0;

                    life.MovePathTimeSumLastCheck = life.LastMove;
                }

                if ((life.LastMove - life.MovePathTimeHackCountLastReset) > 8000)
                {
                    life.MovePathTimeHackCountLastReset = life.LastMove;
                    life.MovePathTimeHackCount = 0;
                }
            }

            return toRet;
        }

        public static void AddAvatar(Packet pPacket, Character pCharacter)
        {
            pPacket.WriteByte(pCharacter.Gender);
            pPacket.WriteByte(pCharacter.Skin);
            pPacket.WriteInt(pCharacter.Face);
            pPacket.WriteByte(0); // Part of equips lol
            pPacket.WriteInt(pCharacter.Hair);
            pCharacter.Inventory.GeneratePlayerPacket(pPacket);
            pPacket.WriteByte(0xFF); // Equips shown end
            pPacket.WriteInt(pCharacter.Inventory.GetEquippedItemId((short)Constants.EquipSlots.Slots.Weapon, true));
        }
    }
}