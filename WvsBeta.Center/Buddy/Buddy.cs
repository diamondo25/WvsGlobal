using System;
using WvsBeta.Common.Sessions;

namespace WvsBeta.Center
{

    public class BuddyData
    {
        public readonly int charId;
        public readonly string charName;

        public BuddyData(int i, string n)
        {
            charId = i;
            charName = n;
        }

        public BuddyData(Packet pr)
        {
            charId = pr.ReadInt();
            charName = pr.ReadString();
        }

        public void EncodeForTransfer(Packet pw)
        {
            pw.WriteInt(charId);
            pw.WriteString(charName);
        }

        public Character GetChar()
        {
            return CenterServer.Instance.FindCharacter(charId);
        }

        public Boolean IsOnline()
        {
            return GetChar() != null;
        }

        public int GetChannel()
        {
            if (IsOnline())
                return GetChar().ChannelID;
            else
                return -1;
        }

        public BuddyList GetBuddyList()
        {
            if (IsOnline())
                return GetChar().FriendsList;
            else
                return null;
        }

        public Boolean IsVisibleTo(BuddyData other)
        {
            return other.IsOnline() && other.GetBuddyList().HasBuddy(this);
        }

        public void SendPacket(Packet pPacket)
        {
            if (IsOnline())
            {
                GetChar().SendPacket(pPacket);
            }
        }

    }
}
