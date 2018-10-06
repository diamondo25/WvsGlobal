using System;
using System.Drawing;
using WvsBeta.Common.Sessions;

namespace WvsBeta.Common
{
    // Do not change to a struct.
    public class Pos
    {
        public short X { get; set; }
        public short Y { get; set; }
        public Pos()
        {
            X = 0;
            Y = 0;
        }

        public Pos(Pos basePos)
        {
            X = basePos.X;
            Y = basePos.Y;
        }

        public Pos(short x, short y)
        {
            X = x;
            Y = y;
        }

        public Pos(Packet pr, bool isInt = false)
        {
            X = isInt ? (short)pr.ReadInt() : pr.ReadShort();
            Y = isInt ? (short)pr.ReadInt() : pr.ReadShort();
        }

        public override string ToString()
        {
            return $"X: {X} Y: {Y}";
        }

        public static int operator -(Pos p1, Pos p2)
        {
            return (int)(Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2)));
        }

        public static implicit operator Point(Pos p) => new Point(p.X, p.Y);
        
    }

}
