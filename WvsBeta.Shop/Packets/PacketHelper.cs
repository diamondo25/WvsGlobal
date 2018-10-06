using System;
using WvsBeta.Common.Sessions;
using WvsBeta.Game;
using WvsBeta.SharedDataProvider;

namespace WvsBeta.Shop
{
    internal class PacketHelper : BasePacketHelper
    {
        public static void AddGiftList(Packet packet)
        {
            packet.WriteShort(1);
            packet.WriteLong(42); // 10
            packet.WriteInt(1); //14 itemid
            packet.WriteString("wahatatas", 13); //27
            packet.WriteLong(0); //35
            packet.WriteInt(400967355); //39
            packet.WriteByte(2); //40 :D
        }
    }
}
