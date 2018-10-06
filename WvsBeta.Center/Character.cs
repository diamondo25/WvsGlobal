using System.Collections.Generic;
using WvsBeta.Common.Sessions;

namespace WvsBeta.Center
{
    public class Character : Common.CharacterBase
    {
        public byte ChannelID { get; set; }
        public bool isCCing { get; set; }
        public bool isConnectingFromLogin { get; set; }
        public bool InCashShop { get; set; }
        public byte LastChannel { get; set; }

        public BuddyList FriendsList { get; set; }

        public Messenger Messenger { get; set; }
        public byte MessengerSlot { get; set; }

        public int WeaponStickerID { get; set; }

        public Dictionary<byte, int> Equips { get; set; }
        private int _PartyID;
        public override int PartyID
        {
            get
            {
                return _PartyID;
            }
            set
            {
                _PartyID = value;
                if (IsOnline)
                {
                    Packet packet = new Packet(ISServerMessages.ChangeParty);
                    packet.WriteInt(ID);
                    packet.WriteInt(_PartyID);
                    CenterServer.Instance.SendPacketToServer(packet, ChannelID);
                }
            }
        }

        public Character() { }

        public Character(Packet pr)
        {
            ChannelID = pr.ReadByte();
            LastChannel = pr.ReadByte();
            FriendsList = new BuddyList(pr);
            base.DecodeForTransfer(pr);
        }

        public new void EncodeForTransfer(Packet pw)
        {
            pw.WriteByte(ChannelID);
            pw.WriteByte(LastChannel);
            FriendsList.EncodeForTransfer(pw);

            base.EncodeForTransfer(pw);
        }

        public void SendPacket(Packet pPacket)
        {
            Packet toserver = new Packet(ISServerMessages.PlayerSendPacket);
            toserver.WriteInt(base.ID);
            toserver.WriteBytes(pPacket.ToArray());
            CenterServer.Instance.SendPacketToServer(toserver, ChannelID);
        }
    }
}
