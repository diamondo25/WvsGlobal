using System.Runtime.InteropServices;

namespace WvsBeta.Game
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Foothold
    {
        public ushort ID;
        public ushort PreviousIdentifier;
        public ushort NextIdentifier;
        public short X1;
        public short Y1;
        public short X2;
        public short Y2;
    }
}