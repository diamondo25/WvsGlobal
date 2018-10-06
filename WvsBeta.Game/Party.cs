using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WvsBeta.Common;

namespace WvsBeta.Game
{
    public class Party
    {
        
        public static Dictionary<int, Party> pParty = new Dictionary<int, Party>();
        
        public List<Character> Members { get; private set; }
        public Character Leader { get { return Members[0]; } }
        public int ID { get; protected set; }

        public Party()
        {
            ID = Server.Instance.PartyIDs.NextValue();
            pParty.Add(ID, this);
        }
        
        public static Party CreateParty(Character pLeader)
        {
            Party party = new Party();
            pLeader.PartyID = party.ID;
            party.Members = new List<Character>();
            party.Members.Add(pLeader);
            party.InsertID(pLeader, party); 
            return party;    
        }

        public void DisbandParty()
        {
            foreach (Character member in Members)
            {
                Members.Remove(member);
            }
        }

        public void InsertID(Character chr, Party party) //This is needed because the server creates a new instance of your character when you log in
        {
            Server.Instance.CharacterDatabase.RunQuery("SELECT `party` FROM characters WHERE ID = '" + chr.ID + "'");
            Server.Instance.CharacterDatabase.RunQuery("UPDATE characters SET party =  " + party.ID + " WHERE ID = " + chr.ID + "");
        }
        public void PartyNull(int id)
        {
            Server.Instance.CharacterDatabase.RunQuery("SELECT `party` FROM characters WHERE ID = '" + id + "'");
            Server.Instance.CharacterDatabase.RunQuery("UPDATE characters SET party = -1 WHERE ID = " + id + "");
        }
        public bool IsLeader(Character chr)
        {
            if (chr.ID == Leader.ID)
            {
                return true;
            }
            return false;
        }

        public void UpdatePartyMemberHP(Character chr)
        {
            foreach(Character member in chr.mParty.Members)
            {
                if (member.Map == chr.Map) //TODO : channel check
                member.sendPacket(PartyPacket.ReceivePartyMemberHP(chr.PrimaryStats.HP, chr.PrimaryStats.MaxHP, chr.ID));
            }
        }
        public void ReceivePartyMemberHP(Character chr)
        {
            foreach (Character member in chr.mParty.Members)
            {
                if (member.Map == chr.Map) //TODO : channel check
                chr.sendPacket(PartyPacket.ReceivePartyMemberHP(member.PrimaryStats.HP, member.PrimaryStats.MaxHP, member.ID));
            }
        }

        public void SendPQSign(Character chr, bool clear)
        {
            string Sound;
            string Message;
            if (clear)
            {
                Sound = "Party1/Clear";
                Message = "quest/party/clear";
            }
            else
            {
                Sound = "Party1/Failed";
                Message = "quest/party/wrong_kor";
            }
            MapPacket.PQMessages(chr, 4, Sound);
            MapPacket.PQMessages(chr, 3, Message);
        }
    }
}

