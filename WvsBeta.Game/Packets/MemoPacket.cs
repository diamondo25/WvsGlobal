using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WvsBeta.Common.Sessions;
using WvsBeta.Database;

namespace WvsBeta.Game
{
    /*
     * Memos, also known as notes.
     */
    class MemoPacket
    {
        public static void OnPacket(Character chr, Packet packet)
        {
            if (packet.ReadByte() == 0) //on delete?
            {
                byte notes = packet.ReadByte();
                int[] noteids = new int[notes];
                for (int i = 0; i < notes; ++i)
                    noteids[i] = packet.ReadInt();
                

                for (int i = 0; i < noteids.Length; i++)
                    Server.Instance.CharacterDatabase.RunQuery("UPDATE memo SET deleted = 1, timeread = " + DateTime.Now.ToFileTime() + " WHERE id = " + noteids[i] + " AND `to` = '" + chr.Name + "'");
            }
        }

        public static byte[] Memos(Memo[] memos)
        {
            Packet packet = new Packet();
            packet.WriteByte(0x1B);
            packet.WriteByte(1);
            packet.WriteByte((byte)memos.Length);
            foreach (Memo memo in memos)
            {
                packet.WriteInt(memo.id);
                packet.WriteString(memo.from);
                packet.WriteString(memo.message);
                packet.WriteLong(memo.time);
            }
            return packet.ToArray();
        }

        public static byte[] SentMemoSuccess()
        {
            Packet packet = new Packet();
            packet.WriteByte(0x1B);
            packet.WriteByte(2);
            return packet.ToArray();
        }

        //0 : online, please whisper
        //1 : check the name of the character
        //2 : inbox full
        public static byte[] SentMemoFailure(byte reason)
        {
            Packet packet = new Packet();
            packet.WriteByte(0x1B);
            packet.WriteByte(3);
            packet.WriteByte(reason);
            return packet.ToArray();
        }
    }

    public class Memo
    {
        public int id;
        public string from, message;
        public long time;

        public Memo(int id, string from, string message, long time)
        {
            this.id = id;
            this.from = from;
            this.message = message;
            this.time = time;
        }
    }
}



