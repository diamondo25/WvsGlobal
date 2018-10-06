using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using WvsBeta.Common.Sessions;

namespace WvsBeta.Game
{
    public enum MessengerFunction : byte
    {
        SelfEnterResult = 0x00,
        Enter = 0x01,
        Leave = 0x02,
        Invite = 0x03,
        InviteResult = 0x04,
        Blocked = 0x05,
        Chat = 0x06,
        Avatar = 0x07,
        Migrated = 0x08,
    }

    public class MessengerRoom
    {
        public int roomID { get; set; }
        public Dictionary<byte, Character> roomSlots = new Dictionary<byte, Character>();

        public MessengerRoom(int roomid)
        {
            roomID = roomid;
            roomSlots.Add(0, null);
            roomSlots.Add(1, null);
            roomSlots.Add(2, null);
        }

        public byte FirstAvailableSlot()
        {
            foreach (KeyValuePair<byte, Character> pair in roomSlots)
                if (pair.Value == null)
                    return pair.Key;
            return 0;
        }

        public byte GetPosition(int cid)
        {
            foreach (KeyValuePair<byte, Character> chr in roomSlots)
                if (chr.Value != null)
                    if (chr.Value.ID == cid)
                        return chr.Key;
            return 0;
        }

        public int GetNumberOfPlayers()
        {
            int ret = 0;
            foreach (Character ch in roomSlots.Values)
                if (ch != null)
                    ret++;
            return ret;
        }

        public byte[] SelfEnterResult(int deliverto, byte slot, Character chr)//int cid, string name, byte gender, byte skin, int face, int hair, Dictionary<byte, int> shownequips
        {
            roomSlots[slot] = chr;

            Packet packet = new Packet(ISServerMessages.PlayerSendPacket);
            packet.WriteInt(deliverto);
            packet.WriteByte(0xAB);
            packet.WriteByte((byte)MessengerFunction.SelfEnterResult);
            packet.WriteByte(slot);
            AddMessengerSlot(packet, chr, true);
            return packet.ToArray();
        }

        public byte[] Enter(int deliverto, Character chr, byte slot)
        {
            roomSlots[slot] = chr;

            Packet packet = new Packet(ISServerMessages.PlayerSendPacket);
            packet.WriteInt(deliverto);
            packet.WriteByte(0xAB);
            packet.WriteByte((byte)MessengerFunction.Enter);
            packet.WriteByte(slot);
            return packet.ToArray();
        }

        public byte[] Leave(int deliverto, byte slot)
        {
            roomSlots[slot] = null;

            Packet packet = new Packet(ISServerMessages.PlayerSendPacket);
            packet.WriteInt(deliverto);
            packet.WriteByte(0xAB);
            packet.WriteByte((byte)MessengerFunction.Leave);
            packet.WriteByte(slot);
            return packet.ToArray();
        }

        public byte[] Invite(int deliverto, Character sender)
        {
            Packet packet = new Packet(ISServerMessages.PlayerSendPacket);
            packet.WriteInt(deliverto);
            packet.WriteByte(0xAB);
            packet.WriteByte((byte)MessengerFunction.Invite);
            packet.WriteString(sender.Name);
            packet.WriteByte(0); //stored, pushed, and never used?! wtf nexon =_="
            packet.WriteInt(roomID);
            return packet.ToArray();
        }

        //values for mode:
        //0 : '%' can't be found.
        //1 : you have sent invite to '%'.
        public byte[] InviteResult(int deliverto, string reciever, byte mode)
        {
            Packet packet = new Packet(ISServerMessages.PlayerSendPacket);
            packet.WriteInt(deliverto);
            packet.WriteByte(0xAB);
            packet.WriteByte((byte)MessengerFunction.InviteResult);
            packet.WriteString(reciever);
            packet.WriteByte(mode);
            return packet.ToArray();
        }

        //values for mode:
        //0 : % denied the request.
        //1 : '%' is currently not accepting chat.
        public byte[] Blocked(int deliverto, string reciever, byte mode)
        {
            Packet packet = new Packet(ISServerMessages.PlayerSendPacket);
            packet.WriteInt(deliverto);
            packet.WriteByte(0xAB);
            packet.WriteByte((byte)MessengerFunction.Blocked);
            packet.WriteString(reciever);
            packet.WriteByte(mode);
            return packet.ToArray();
        }

        public byte[] Chat(int deliverto, string message, int cidfrom)
        {
            Packet packet = new Packet(ISServerMessages.PlayerSendPacket);
            packet.WriteInt(deliverto);
            packet.WriteByte(0xAB);
            packet.WriteByte((byte)MessengerFunction.Chat);
            packet.WriteString(message);//Must be in format of 'name : message'
            return packet.ToArray();
        }

        public byte[] Avatar(int deliverto, byte slot, Character chr)
        {
            Packet packet = new Packet(ISServerMessages.PlayerSendPacket);
            packet.WriteInt(deliverto);
            packet.WriteByte(0xAB);
            packet.WriteByte((byte)MessengerFunction.Avatar);
            packet.WriteByte(slot);
            AddMessengerSlot(packet, chr, false);
            return packet.ToArray();
        }

        public byte[] Migrated(int deliverto, Dictionary<byte, Character> chars)
        {
            Packet packet = new Packet(ISServerMessages.PlayerSendPacket);
            packet.WriteInt(deliverto);
            packet.WriteByte(0xAB);
            packet.WriteByte((byte)MessengerFunction.Migrated);
            foreach (KeyValuePair<byte, Character> chr in chars)
            {
                packet.WriteByte(chr.Key);//0 = delete, 1 = no change, 2 = add
                if (chr.Key == 2)
                    AddMessengerSlot(packet, chr.Value, false);
            }
            return packet.ToArray();
        }

        /*
         * 01 //slot
         * 00 //gender
         * 00 //skin
         * 20 4E 00 00 //face
         * 00 //smega
         * 44 75 00 00 //hair 
         * FF //end equips
         * 00 00 00 00 //petid
         * 05 00 55 55 55 55 55 //char name
         * 06 //channel?
         * 00 //show in chat
         */
        public static void AddMessengerSlot(Packet packet, Character chr, bool inChat)
        {
            //AddAvatar(packet, chr.Gender, chr.Skin, chr.Face, chr.Hair, chr.Equips);
            packet.WriteString(chr.Name);
            //packet.WriteByte(chr.ChannelID);
            packet.WriteBool(inChat);
        }

        public static void AddAvatar(Packet packet, byte gender = 0, byte skin = 0, int face = 20000, int hair = 30020, Dictionary<byte, int> shownequips = null)
        {
            packet.WriteByte(gender);
            packet.WriteByte(skin);
            packet.WriteInt(face);
            packet.WriteByte(0);//smega
            packet.WriteInt(hair);
            if (shownequips != null)
                foreach (KeyValuePair<byte, int> kvp in shownequips)
                {
                    packet.WriteByte(kvp.Key);
                    packet.WriteInt(kvp.Value);
                }
            packet.WriteByte(0xFF);
            packet.WriteInt(0);//pet id
        }
    }
}
