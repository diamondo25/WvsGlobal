using System.Collections.Generic;
using WvsBeta.Common.Sessions;

namespace WvsBeta.Game
{

    public static class QuestPacket
    {

        public static void SendQuestDataUpdate(Character chr, int QuestID, string Data)
        {
            Packet pw = new Packet(ServerMessages.SHOW_STATUS_INFO);
            pw.WriteByte(0x01);
            pw.WriteBool(true);
            pw.WriteInt(QuestID);
            pw.WriteString(Data);
            chr.SendPacket(pw);
        }

        public static void SendQuestRemove(Character chr, int QuestID)
        {
            Packet pw = new Packet(ServerMessages.SHOW_STATUS_INFO);
            pw.WriteByte(0x01);
            pw.WriteBool(false);
            pw.WriteInt(QuestID);
            chr.SendPacket(pw);
        }

        public static void SendGainItemChat(Character chr, params KeyValuePair<int, int>[] pItems)
        {
            Packet pw = new Packet(ServerMessages.PLAYER_EFFECT);
            pw.WriteByte(0x03);
            pw.WriteByte((byte)pItems.Length);
            foreach (var kvp in pItems)
            {
                pw.WriteInt(kvp.Key);
                pw.WriteInt(kvp.Value);
            }
            chr.SendPacket(pw);
        }
    }
}
