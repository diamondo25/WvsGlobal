namespace WvsBeta.Game
{
    public class Seat
    {
        public Seat(byte sid, short xLoc, short yLoc)
        {
            ID = sid;
            X = xLoc;
            Y = yLoc;
        }
        public readonly byte ID;
        public readonly short X;
        public readonly short Y;
    }
}