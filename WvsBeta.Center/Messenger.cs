using System;
using System.Collections.Generic;
using System.Linq;
using WvsBeta.Center.CharacterPackets;
using WvsBeta.Common.Sessions;

namespace WvsBeta.Center
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

    public class Messenger
    {
        public static List<Messenger> Messengers = new List<Messenger>();

        public int ID { get; set; }
        public Character Owner { get; set; }
        public Character[] Users { get; set; }
        public const int MAX_USERS = 3;

        public Messenger(Character pOwner)
        {
            Users = new Character[MAX_USERS];
            Owner = pOwner;
            ID = pOwner.ID;
            AddPlayer(pOwner);
            Messengers.Add(this);
            pOwner.Messenger = this;
        }

        public static void EncodeForMigration(Packet pw)
        {
            pw.WriteInt(Messengers.Count);
            foreach (var messenger in Messengers)
            {
                pw.WriteInt(messenger.ID);
                for (var i = 0; i < MAX_USERS; i++)
                {
                    pw.WriteInt(messenger.Users[i]?.ID ?? -1);
                }
            }
        }

        public static void DecodeForMigration(Packet pr)
        {
            var amount = pr.ReadInt();

            var charids = new int[MAX_USERS];
            for (var i = 0; i < amount; i++)
            {
                var ownerId = pr.ReadInt();
                for (var j = 0; j < MAX_USERS; j++)
                {
                    charids[j] = pr.ReadInt();
                }

                var owner = CenterServer.Instance.FindCharacter(ownerId);
                if (owner != null)
                {
                    var messenger = new Messenger(owner);
                    for (byte j = 0; j < MAX_USERS; j++)
                    {
                        var character = messenger.Users[j] = CenterServer.Instance.FindCharacter(charids[j]);
                        // Re-assign user
                        if (character != null)
                        {
                            character.Messenger = messenger;
                            character.MessengerSlot = j;
                        }
                    }
                }
            }
        }

        public static void JoinMessenger(Packet packet)
        {
            int messengerID = packet.ReadInt();
            Character chr = ParseMessengerCharacter(packet);

            if (messengerID > 0 && Messengers.Exists(m => m.ID == messengerID))
            {
                JoinExistingMessenger(messengerID, chr);
            }
            else
            {
                CreateMessenger(chr);
            }
        }


        private static void CreateMessenger(Character pOwner)
        {
            Messenger messenger = new Messenger(pOwner);
            pOwner.SendPacket(MessengerPacket.Enter(pOwner.MessengerSlot));
        }

        private static void JoinExistingMessenger(int messengerID, Character chr)
        {
            Messenger messenger = Messengers.First(m => m.ID == messengerID);
            if (messenger == null) // This should already be confirmed when joining, but just to make sure.
            {
                return;
            }
            if (messenger.AddPlayer(chr)) // No action if messenger is full afaik.
            {
                chr.Messenger = messenger;
                foreach (Character mChr in messenger.Users)
                {

                    if (mChr == null) continue;

                    if (mChr.ID == chr.ID)
                    {
                        chr.SendPacket(MessengerPacket.Enter(chr.MessengerSlot));
                    }
                    else
                    {
                        chr.SendPacket(MessengerPacket.SelfEnter(mChr)); // Announce existing players to joinee
                        mChr.SendPacket(MessengerPacket.SelfEnter(chr)); // Announce joinee to existing players
                    }
                }
            }
        }

        public static void LeaveMessenger(int cid)
        {
            Character chr = CenterServer.Instance.FindCharacter(cid);
            Messenger messenger = chr.Messenger;

            if (messenger == null)
            {
                return;
            }

            byte slot = chr.MessengerSlot;
            bool empty = true;

            foreach (Character mChr in messenger.Users)
            {
                if (mChr != null)
                {
                    if (mChr.ID != chr.ID)
                    {
                        empty = false;
                    }
                    mChr.SendPacket(MessengerPacket.Leave(slot));
                }
            }

            messenger.Users[slot] = null;
            chr.Messenger = null;
            chr.MessengerSlot = 0;

            if (empty)
            {
                Messengers.Remove(messenger);
            }
        }

        private int usersInChat()
        {
            return Users.Count(e => e != null); // Max was here
        }

        public static void SendInvite(int senderID, String recipientName)
        {
            Character recipient = CenterServer.Instance.FindCharacter(recipientName);
            Character sender = CenterServer.Instance.FindCharacter(senderID);
            Messenger messenger = sender.Messenger;

            if (sender == null || messenger == null || messenger.usersInChat() >= MAX_USERS)
            {
                return;
            }
            else if (recipient != null)
            {
                recipient.SendPacket(MessengerPacket.Invite(sender.Name, messenger.ID));
            }

            sender.SendPacket(MessengerPacket.InviteResult(recipientName, recipient != null));
        }

        public static void Chat(int cid, String message)
        {
            Character chr = CenterServer.Instance.FindCharacter(cid);

            if (chr.Messenger == null)
            {
                return;
            }

            foreach (Character mChr in chr.Messenger.Users)
            {
                if (mChr != null && mChr.ID != cid)
                {
                    mChr.SendPacket(MessengerPacket.Chat(message));
                }
            }
        }

        private static Character ParseMessengerCharacter(Packet packet)
        {
            Character pCharacter = CenterServer.Instance.FindCharacter(packet.ReadInt());

            pCharacter.Name = packet.ReadString();
            pCharacter.Gender = packet.ReadByte();
            pCharacter.Skin = packet.ReadByte();
            pCharacter.Face = packet.ReadInt();
            packet.ReadByte();
            pCharacter.Hair = packet.ReadInt();

            var equips = new Dictionary<byte, int>();
            while (true)
            {
                byte slot = packet.ReadByte();
                if (slot == 0xFF)
                    break;
                int itemid = packet.ReadInt();
                equips[slot] = itemid;
            }
            pCharacter.Equips = equips;

            pCharacter.WeaponStickerID = packet.ReadInt();
            return pCharacter;
        }

        public bool AddPlayer(Character pCharacter)
        {
            byte slot = (byte)Array.IndexOf(Users, null);
            if (slot < MAX_USERS)
            {
                pCharacter.MessengerSlot = slot;
                Users[slot] = pCharacter;
                return true;
            }
            return false;
        }

        public static void Block(Packet packet)
        {
            //TODO
        }

        public static void OnAvatar(Packet packet)
        {
            Character chr = ParseMessengerCharacter(packet); //, chr, false);
            Messenger messenger = chr.Messenger;

            foreach (var c in messenger.Users)
            {
                c.SendPacket(MessengerPacket.Avatar(chr));
            }
        }
    }
}
