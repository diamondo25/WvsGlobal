using System;
using System.Collections.Generic;
using WvsBeta.Common.Sessions;

namespace WvsBeta.Center.CharacterPackets
{

    enum MessengerAction : byte
    {
        SelfEnterResult = 0,
        Enter = 1,
        Leave = 2,
        Invite = 3,
        InviteResult = 4,
        Blocked = 5,
        Chat = 6,
        Avatar = 7,
        Migrated = 8,
    }

    public static class MessengerPacket
    {
        // Used for visually displaying Characters in messenger
        public static Packet SelfEnter(Character chr)
        {
            Packet packet = new Packet(ServerMessages.MESSENGER);
            packet.WriteByte((byte)MessengerAction.SelfEnterResult);
            ModifyMessengerSlot(packet, chr, true);
            return packet;
        }

        //Used to inform the client which slot it's going to enter
        public static Packet Enter(byte slot)
        {
            Packet packet = new Packet(ServerMessages.MESSENGER);
            packet.WriteByte((byte)MessengerAction.Enter);
            packet.WriteByte(slot);
            return packet;
        }

        public static Packet Leave(byte slot)
        {
            Packet packet = new Packet(ServerMessages.MESSENGER);
            packet.WriteByte((byte)MessengerAction.Leave);
            packet.WriteByte(slot);
            return packet;
        }

        public static Packet Invite(String sender, int messengerId)
        {
            Packet packet = new Packet(ServerMessages.MESSENGER);
            packet.WriteByte((byte)MessengerAction.Invite);
            packet.WriteString(sender);
            packet.WriteByte(0);
            packet.WriteInt(messengerId);
            packet.WriteByte(0);
            return packet;
        }

        public static Packet InviteResult(String recipient, bool success)
        {
            Packet packet = new Packet(ServerMessages.MESSENGER);
            packet.WriteByte((byte)MessengerAction.InviteResult);
            packet.WriteString(recipient);
            packet.WriteBool(success); // False : '%' can't be found. True : you have sent invite to '%'.
            return packet;
        }

        public static Packet Blocked(int deliverto, string receiver, byte mode)
        {
            Packet packet = new Packet(ServerMessages.MESSENGER);
            packet.WriteByte((byte)MessengerFunction.Blocked);
            packet.WriteString(receiver);
            packet.WriteByte(mode); // 0 : % denied the request. 1 : '%' is currently not accepting chat.
            return packet;
        }

        public static Packet Chat(string message)
        {
            Packet packet = new Packet(ServerMessages.MESSENGER);
            packet.WriteByte((byte)MessengerFunction.Chat);
            packet.WriteString(message);
            return packet;
        }

        public static Packet Avatar(Character chr)
        {
            Packet packet = new Packet(ServerMessages.MESSENGER);
            packet.WriteByte((byte)MessengerFunction.Avatar);
            packet.WriteByte(chr.MessengerSlot);
            ModifyMessengerSlot(packet, chr, false);
            return packet;
        }
        
        private static void ModifyMessengerSlot(Packet packet, Character chr, bool InChat)
        {
            packet.WriteByte(chr.MessengerSlot);
            AddAvatar(packet, chr);
            packet.WriteString(chr.Name);
            packet.WriteByte(chr.ChannelID);
            packet.WriteBool(InChat); //Announce in chat
        }
        
        // This is super wonky, we should utilize PacketHelper.AddAvatar for this, but it's in the WvsBeta.Game namespace, so it's not available..
        private static void AddAvatar(Packet packet, Character pCharacter)
        {
            packet.WriteByte(pCharacter.Gender);
            packet.WriteByte(pCharacter.Skin);
            packet.WriteInt(pCharacter.Face);
            packet.WriteByte(0); // Part of equips lol
            packet.WriteInt(pCharacter.Hair);
            foreach (var kvp in pCharacter.Equips)
            {
                packet.WriteByte(kvp.Key);
                packet.WriteInt(kvp.Value);
            }
            packet.WriteByte(0xFF); // Equips shown end
            packet.WriteInt(pCharacter.WeaponStickerID);
            // Eventually this will contain pet item ID
        }
    }
}
