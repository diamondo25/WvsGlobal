using System;
using System.Collections.Generic;
using System.Linq;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;

namespace WvsBeta.Game
{
    public partial class CenterSocket
    {

        public void CreateParty(int cid)
        {
            Packet pw = new Packet(ISClientMessages.PartyCreate);
            pw.WriteInt(cid);
            SendPacket(pw);
        }

        public void InviteToParty(int from, int to)
        {
            Packet pw = new Packet(ISClientMessages.PartyInvite);
            pw.WriteInt(from);
            pw.WriteInt(to);
            SendPacket(pw);
        }

        public void ExpelFromParty(int leader, int fucker)
        {
            Packet pw = new Packet(ISClientMessages.PartyExpel);
            pw.WriteInt(leader);
            pw.WriteInt(fucker);
            SendPacket(pw);
        }

        public void AcceptParty(int acceptor)
        {
            Packet pw = new Packet(ISClientMessages.PartyAccept);
            pw.WriteInt(acceptor);
            SendPacket(pw);
        }

        public void LeaveParty(int leaver)
        {
            Packet pw = new Packet(ISClientMessages.PartyLeave);
            pw.WriteInt(leaver);
            SendPacket(pw);
        }

        public void DeclineParty(int declId, string declName)
        {
            Packet pw = new Packet(ISClientMessages.PartyDecline);
            pw.WriteInt(declId);
            pw.WriteString(declName);
            SendPacket(pw);
        }

        public void PartyChat(int blabber, string msg)
        {
            Packet pw = new Packet(ISClientMessages.PartyChat);
            pw.WriteInt(blabber);
            pw.WriteString(msg);
            SendPacket(pw);
        }

        public void PartyDoorCreated(MysticDoor door)
        {
            Packet pw = new Packet(ISClientMessages.PartyDoorChanged);
            pw.WriteInt(door.OwnerId);
            pw.WriteInt(DataProvider.Maps[door.FieldId].ReturnMap);
            pw.WriteInt(door.FieldId);
            pw.WriteShort(door.X);
            pw.WriteShort(door.Y);
            SendPacket(pw);
        }

        public void PartyDoorRemoved(int charid)
        {
            Packet pw = new Packet(ISClientMessages.PartyDoorChanged);
            pw.WriteInt(charid);
            pw.WriteInt(Constants.InvalidMap);
            pw.WriteInt(Constants.InvalidMap);
            pw.WriteShort(0);
            pw.WriteShort(0);
            SendPacket(pw);
        }

        public void SendUpdateRates()
        {
            Packet ret = new Packet(ISClientMessages.ChangeRates);
            ret.WriteDouble(Server.Instance.RateMobEXP);
            ret.WriteDouble(Server.Instance.RateMesoAmount);
            ret.WriteDouble(Server.Instance.RateDropChance);
            SendPacket(ret);
        }

        public void SendUpdateConnections(int value)
        {
            Packet packet = new Packet(ISClientMessages.ServerSetConnectionsValue);
            packet.WriteInt(value);
            SendPacket(packet);
        }

        public void RequestCharacterConnectToWorld(string Hash, int charid, byte world, byte channel, Character character)
        {
            Packet packet = new Packet(ISClientMessages.PlayerChangeServer);
            packet.WriteString(Hash);
            packet.WriteInt(charid);
            packet.WriteByte(world);
            packet.WriteByte(channel);
            packet.WriteBool(true);

            // Add the character buffs
            character.PrimaryStats.EncodeForCC(packet);
            // Add the summons
            character.Summons.EncodeForCC(packet);

            SendPacket(packet);
        }

        public void UnregisterCharacter(int charid, bool cc)
        {
            Packet packet = new Packet(ISClientMessages.ServerRegisterUnregisterPlayer);
            packet.WriteInt(charid);
            packet.WriteBool(false);
            packet.WriteBool(cc);
            SendPacket(packet);
        }

        public void RegisterCharacter(int charid, string name, short job, byte level, byte gm)
        {
            Packet packet = new Packet(ISClientMessages.ServerRegisterUnregisterPlayer);
            packet.WriteInt(charid);
            packet.WriteBool(true);
            packet.WriteString(name);
            packet.WriteShort(job);
            packet.WriteByte(level);
            packet.WriteByte(gm);
            SendPacket(packet);
        }

        public void RequestBuddyListLoad(string charname, bool test, int buddycapacity)
        {
            Packet packet = new Packet(ISClientMessages.RequestBuddylist);
            packet.WriteString(charname);
            packet.WriteBool(test);
            packet.WriteInt(buddycapacity);
            SendPacket(packet);
        }

        public void PlayerWhisper(int charid, string name, string message)
        {
            Packet packet = new Packet(ISClientMessages.PlayerWhisperOrFindOperation);
            packet.WriteInt(charid);
            packet.WriteBool(true);
            packet.WriteString(name);
            packet.WriteString(message);
            SendPacket(packet);
        }

        public void BuddyUpdate(Character chr)
        {
            Packet packet = new Packet(ISClientMessages.BuddyUpdate);
            packet.WriteInt(chr.ID);
            packet.WriteString(chr.Name);
            SendPacket(packet);
        }

        public void BuddyRequest(Character requestor, String targetName)
        {
            Packet packet = new Packet(ISClientMessages.BuddyInvite);
            packet.WriteInt(requestor.ID);
            packet.WriteString(requestor.Name);
            packet.WriteString(targetName);
            SendPacket(packet);
        }

        public void BuddyAccept(Character acceptor)
        {
            Packet packet = new Packet(ISClientMessages.BuddyInviteAnswer);
            packet.WriteInt(acceptor.ID);
            packet.WriteString(acceptor.Name);
            SendPacket(packet);
        }

        public void BuddyDecline(Character decliner, int Victim)
        {
            Packet packet = new Packet(ISClientMessages.BuddyDecline);
            packet.WriteInt(decliner.ID);
            packet.WriteInt(Victim);
            SendPacket(packet);
        }

        public void BuddyListExpand(Character chr)
        {
            Packet packet = new Packet(ISClientMessages.BuddyListExpand);
            packet.WriteInt(chr.ID);
            SendPacket(packet);
        }

        public void AdminMessage(string message, byte type)
        {
            Packet packet = new Packet(ISClientMessages.AdminMessage);
            packet.WriteString(message);
            packet.WriteByte(type);
            SendPacket(packet);
        }
        public void PlayerFind(int charid, string name)
        {
            Packet packet = new Packet(ISClientMessages.PlayerWhisperOrFindOperation);
            packet.WriteInt(charid);
            packet.WriteBool(false);
            packet.WriteString(name);
            SendPacket(packet);
        }

        public void FindPlayerInOtherGameServer(string name)
        {
            Packet packet = new Packet(ISClientMessages.FindPlayer);
            packet.WriteString(name);
            SendPacket(packet);
        }

        public void PlayerSuperMegaphone(string message, bool whisperetc)
        {
            Packet packet = new Packet(ISClientMessages.PlayerUsingSuperMegaphone);
            packet.WriteString(message);
            packet.WriteBool(whisperetc);
            packet.WriteByte(Server.Instance.ID);
            SendPacket(packet);
        }


        public void MessengerJoin(int messengerid, Character chr)
        {
            Packet packet = new Packet(ISClientMessages.MessengerJoin);
            packet.WriteInt(messengerid);
            packet.WriteInt(chr.ID);
            packet.WriteString(chr.Name);
            PacketHelper.AddAvatar(packet, chr);
            SendPacket(packet);
        }

        public void MessengerLeave(int cid)
        {
            Packet packet = new Packet(ISClientMessages.MessengerLeave);
            packet.WriteInt(cid);
            SendPacket(packet);
        }

        public void MessengerInvite(int cid, string cinvitee)
        {
            Packet packet = new Packet(ISClientMessages.MessengerInvite);
            packet.WriteInt(cid);
            packet.WriteString(cinvitee);
            SendPacket(packet);
        }

        /*public void MessengerDecline(String recipient, String sender)
        {
            Packet packet = new Packet(ISClientMessages.MessengerDeclined);
            packet.WriteString(recipient);
            packet.WriteString(sender);
            SendPacket(packet);
        }*/

        public void MessengerBlock(int cid, string invitee, string inviter, byte blockmode)
        {
            Packet packet = new Packet(ISClientMessages.MessengerBlocked);
            packet.WriteInt(cid);
            packet.WriteString(invitee);
            packet.WriteString(inviter);
            packet.WriteByte(blockmode);
            SendPacket(packet);
        }

        public void MessengerChat(int cid, string chatmsg)
        {
            Packet packet = new Packet(ISClientMessages.MessengerChat);
            packet.WriteInt(cid);
            packet.WriteString(chatmsg);
            SendPacket(packet);
        }

        public void MessengerAvatar(Character chr)
        {
            Packet packet = new Packet(ISClientMessages.MessengerAvatar);
            packet.WriteInt(chr.ID);
            packet.WriteString(chr.Name);
            PacketHelper.AddAvatar(packet, chr);
            SendPacket(packet);
        }

        public void PlayerUpdateMap(Character pCharacter)
        {
            Packet packet = new Packet(ISClientMessages.PlayerUpdateMap);
            packet.WriteInt(pCharacter.ID);
            packet.WriteInt(pCharacter.MapID);
            packet.WriteInt(pCharacter.PartyID);
            SendPacket(packet);
        }

        public void BuddyChat(Character chr, string what, int[] recipients)
        {
            Packet packet = new Packet(ISClientMessages.BuddyChat);
            packet.WriteInt(chr.ID);
            packet.WriteString(chr.Name);
            packet.WriteString(what);
            packet.WriteByte((byte)recipients.Length);
            recipients.ForEach(packet.WriteInt);
            SendPacket(packet);
        }

        public void KickUser(int uid)
        {
            // Check local server first
            var localPlayers = Server.Instance.CharacterList.Where(x => x.Value.UserID == uid).ToArray();

            if (localPlayers.Length > 0)
            {
                Program.MainForm.LogAppend("Handling kick request for user " + uid);
                localPlayers.ForEach(x =>
                {
                    x.Value.Player.Socket.Disconnect();
                });
            }
            else
            {
                Packet packet = new Packet(ISClientMessages.KickPlayer);
                packet.WriteInt(uid);
                SendPacket(packet);
            }
        }

        public void UpdatePlayerJobLevel(Character chr)
        {
            Packet packet = new Packet(ISClientMessages.UpdatePlayerJobLevel);
            packet.WriteInt(chr.ID);
            packet.WriteShort(chr.Job);
            packet.WriteByte(chr.Level);
            SendPacket(packet);
        }
    }
}
