using System.Runtime.InteropServices;

namespace WvsBeta.Game
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Life
    {
        public int ID { get; set; }
        public int RespawnTime { get; set; }
        public char Type { get; set; }
        public ushort Foothold { get; set; }
        public bool FacesLeft { get; set; }
        public short X { get; set; }
        public short Y { get; set; }
        public short Cy { get; set; }
        public short Rx0 { get; set; }
        public short Rx1 { get; set; }
    }
}