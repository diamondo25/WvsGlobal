using System;
using WvsBeta.Common.Sessions;

namespace WvsBeta.Shop
{
    public static class MessagePacket
    {
        public static void SendCharge(Character victim)
        {
            Packet pw = new Packet(0xB9); // 185, pressumably 188 in v12
            pw.WriteString("ilub");
            pw.WriteString("Diamondo25");
            pw.WriteByte(0);
            pw.WriteShort(0);
            pw.WriteInt(0);
            pw.WriteInt(0);
            victim.SendPacket(pw);
        }

        public static void SendCharge(Character victim, bool derp)
        {
            Packet pw = new Packet(0xBB); // 187, pressumably 190 in v12
            pw.WriteByte(0xBB);
            pw.WriteByte(0x4D);
            pw.WriteString("ilub");
            Random rnd = new Random();
            pw.WriteInt(rnd.Next());
            pw.WriteByte((byte)rnd.Next(0, 0xFF));
            victim.SendPacket(pw);
        }

        public static void SendNotice(string what, Character victim)
        {
            Packet pw = new Packet(ServerMessages.BROADCAST_MSG);
            pw.WriteByte(0);
            pw.WriteString(what);
            victim.SendPacket(pw);
        }

        public static void SendScrollingHeader(string what, Character victim)
        {
            Packet pw = new Packet(ServerMessages.BROADCAST_MSG);
            pw.WriteByte(4);
            pw.WriteBool((what.Length == 0 ? false : true));
            pw.WriteString(what);
            victim.SendPacket(pw);
        }
    }
}
