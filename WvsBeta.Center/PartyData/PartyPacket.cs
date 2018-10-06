using System.Collections.Generic;
using System.Linq;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;

namespace WvsBeta.Center
{
    public enum PartyFunction : byte
    {
        INVITE_DONE = 0x04,                         //done
        LOAD_DONE = 0x06,                           //??? Is this Silent Update?
        CREATE_NEW_DONE = 0x07,                     //done
        CREATE_NEW_ALREADY_JOINED = 0x08,           //done
        CREATE_NEW_BEGINNER_DISALLOWED = 0x09,      //done
        CREATE_NEW_UNK_ERR = 0xA,                   //done
        WITHDRAW_DONE = 0xB,                        //done
        WITHDRAW_NOT_JOINED = 0xC,                  //done
        WITHDRAW_UNK = 0xD,                         //done
        JOIN_DONE = 0xE,                            //done
        JOIN_ALREADY_JOINED = 0xF,                  //done
        JOIN_ALREADY_FULL = 0x10,                   //done
        JOIN_PARTY_UNK_USER = 0x11,                 //useless
        INVITE_BLOCKED = 0x13,                      //TODO blocked invites
        INVITE_USER_ALREADY_HAS_INVITE = 0x14,      //done
        INVITE_REJECTED = 0x15,                     //done
        ADMIN_CANNOT_INVITE = 0x17,                 //useless
        ADMIN_CANNOT_CREATE = 0x18,                 //useless
        UNABLE_TO_FIND_PLAYER = 0x19,               //done
        TOWN_PORTAL_CHANGED = 0x1A,                 //
        CHANGE_LEVEL_OR_JOB = 0x1B,                 //useless
        TOWN_PORTAL_CHANGED_UNK = 0x1C,             //???

        /*
         * THANKS SUNNYBOY ^_^ YOU ARE AWESOME
        LoadParty(0),
        CreateNewParty(1),
        WithdrawParty(2),
        JoinParty(3),
        InviteParty(4),// 
        KickParty(5),
        LoadParty_Done(6),// 
        CreateNewParty_Done(7),// 
        CreateNewParty_AlreayJoined(8), // 
        CreateNewParty_Beginner(9), // 
        CreateNewParty_Unknown(10),
        WithdrawParty_Done(11),// 
        WithdrawParty_NotJoined(12), // //
        WithdrawParty_Unknown(13),// i guess
        JoinParty_Done(14),// 
        JoinParty_AlreadyJoined(15), // 
        JoinParty_AlreadyFull(16), // 
        JoinParty_UnknownUser(17), // 
        InviteParty_BlockedUser(19), //
        InviteParty_AlreadyInvited(20),//
        InviteParty_Rejected(21),//
        AdminCannotCreate(24), //
        AdminCannotInvite(23), //
        UnableToFindPlayer(25), //
        UserMigration(26), //
        ChangeLevelOrJob(27), //
        TownPortalChanged(28); 
        */
    }

    public static class PartyPacket
    {
        public const int CHANNEL_ID_OFFLINE = -2;

           
        public static Packet PartyCreated(int charid, int partyid)
        {
            //20 07 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01
            Packet pw = new Packet(ServerMessages.PARTY_RESULT);
            pw.WriteByte((byte)PartyFunction.CREATE_NEW_DONE);
            pw.WriteInt(partyid); // Party ID
            pw.WriteInt(Constants.InvalidMap); // Mystic Door's town ID
            pw.WriteInt(Constants.InvalidMap);  // Mystic Door's Target ID
            pw.WriteShort(0); // Mystic Door's X Position
            pw.WriteShort(0); // Mystic Door's Y Position
            return pw;
        }

        public static Packet PartyError(PartyFunction Message)
        {
            Packet pw = new Packet(ServerMessages.PARTY_RESULT);
            pw.WriteByte((byte)Message);
            return pw;
        }

        public static Packet PartyErrorWithName(PartyFunction Message, string name)
        {
            Packet pw = new Packet(ServerMessages.PARTY_RESULT);
            pw.WriteByte((byte)Message);
            pw.WriteString(name);
            return pw;
        }

        public static Packet JoinParty(PartyMember joined, Party pt)
        {
            Packet pw = new Packet(ServerMessages.PARTY_RESULT);
            pw.WriteByte((byte)PartyFunction.JOIN_DONE);
            pw.WriteInt(pt.partyId); //pid? charid?
            pw.WriteString(joined.name);
            AddPartyData(pw, joined, pt);
            return pw;
        }
        
        public static Packet SilentUpdate(PartyMember update, Party pt, int disconnecting = -1)
        {
            Packet pw = new Packet(ServerMessages.PARTY_RESULT);
            pw.WriteByte((byte)PartyFunction.LOAD_DONE);
            pw.WriteInt(pt.partyId); //pid? charid?
            AddPartyData(pw, update, pt, null, disconnecting);
            return pw;
        }

        public static Packet MemberLeft(PartyMember sendTo, PartyMember leaving, Party pt, bool disband, bool expel)
        {
            Packet pw = new Packet(ServerMessages.PARTY_RESULT);
            pw.WriteByte((byte)PartyFunction.WITHDRAW_DONE);
            pw.WriteInt(pt.partyId);
            pw.WriteInt(leaving.id);
            pw.WriteBool(!disband); //disband ? 0 : 1
            if (!disband)
            {
                pw.WriteBool(expel);
                pw.WriteString(leaving.name);
                AddPartyData(pw, sendTo, pt, leaving);
            }
            return pw;
        }

        public static Packet PartyInvite(Party pt)
        {
            Packet pw = new Packet(ServerMessages.PARTY_RESULT);
            pw.WriteByte((byte)PartyFunction.INVITE_DONE);
            pw.WriteInt(pt.partyId);
            pw.WriteString(pt.leader.name);
            return pw;
        }

        public static Packet UpdateDoor(DoorInformation door, byte ownerIdIdx)
        {
            Program.MainForm.LogDebug("Updating door at index: " + ownerIdIdx);
            Packet pw = new Packet(ServerMessages.PARTY_RESULT);
            pw.WriteByte((byte)PartyFunction.TOWN_PORTAL_CHANGED);
            pw.WriteByte(ownerIdIdx);
            pw.WriteInt(door.DstMap);
            pw.WriteInt(door.SrcMap);
            pw.WriteShort(door.X);
            pw.WriteShort(door.Y);
            return pw;
        }

        public static void AddPartyData(Packet packet, PartyMember member, Party pt, PartyMember remove = null, int disconnect = -1)
        {
            //226
            var ids = pt.members.Select(e => e == null ? 0 : e.id);
            var names = pt.members.Select(e => e == null ? "" : e.name);
            var maps = pt.members.Select(e => (e == null || e.id == disconnect || e.GetChannel() != member.GetChannel()) ? -1 : e.GetMap());
            var doorTowns = pt.members.Select(e => e == null ? Constants.InvalidMap : e.door.DstMap);

            ids.ForEach(packet.WriteInt);
            names.ForEach(packet.WriteString13);
            maps.ForEach(packet.WriteInt);
            packet.WriteInt(pt.leader.id);
            maps.ForEach(m => packet.WriteInt(-1));

            //doors - these are fucked, fix in v39 i guess
            for (int i = 0; i < Constants.MaxPartyMembers; i++)
            {
                packet.WriteInt(Constants.InvalidMap); 
                packet.WriteInt(Constants.InvalidMap); 
                packet.WriteShort(0);        
                packet.WriteShort(0);  
            }
        }

        // Actual hack; this should be client-sided
        public static Packet NoneOnline() 
        {
            // Red text packet
            Packet pw = new Packet(ServerMessages.BROADCAST_MSG);
            pw.WriteByte(0x05);
            pw.WriteString("Either the party doeesn't exist or no member of your party is logged on.");
            return pw;
        }

        public static Packet PartyChat(string fromName, string text, byte group)
        {
            Packet pw = new Packet(ServerMessages.GROUP_MESSAGE);
            pw.WriteByte(group);
            pw.WriteString(fromName);
            pw.WriteString(text);
            return pw;
        }

        public static Packet RequestHpUpdate(int id)
        {
            Packet pw = new Packet(ISServerMessages.UpdateHpParty);
            pw.WriteInt(id);
            return pw;
        }
    }
}
