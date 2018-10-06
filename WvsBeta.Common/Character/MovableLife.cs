using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WvsBeta.Common
{
    public class MovableLife
    {
        public byte Stance { get; set; }
        public short Foothold { get; set; }
        public Pos Position { get; set; }
        public Pos Wobble { get; set; }
        public byte Jumps { get; set; }
        public long LastMove { get; set; }

        public long MovePathTimeSumLastCheck { get; set; }
        public long MovePathTimeSum { get; set; }
        public long MovePathTimeHackCountLastReset { get; set; }
        public int MovePathTimeHackCount { get; set; }

        public MovableLife()
        {
            Stance = 0;
            Foothold = 0;
            Position = new Pos();
            Wobble = new Pos(0, 0);
        }

        public MovableLife(short pFH, Pos pPosition, byte pStance)
        {
            Stance = pStance;
            Position = new Pos(pPosition);
            Foothold = pFH;
            Wobble = new Pos(0, 0);
            MovePathTimeHackCountLastReset =
                MovePathTimeSumLastCheck =
                    LastMove = MasterThread.CurrentTime;
        }

        public bool IsFacingRight()
        {
            return Stance % 2 == 0;
        }
    }
}
