using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using MySql.Data.MySqlClient;
using WvsBeta.Center.DBAccessor;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;

namespace WvsBeta.Center
{
    public class LocalConnection : AbstractConnection
    {
        private static ILog _log = LogManager.GetLogger("LocalConnection");

        public static WorldServer World => CenterServer.Instance.World;

        public LocalServer Server { get; set; }
        public static List<Messenger> MessengerRooms { get; set; }

        public LocalConnection(System.Net.Sockets.Socket pSocket) : base(pSocket) { }

        public void Init()
        {
            Pinger.Add(this);
            SendHandshake(1, "WvsBeta Server", 8);
        }

        public void SendRates()
        {
            Packet packet = new Packet(ISServerMessages.ChangeRates);
            packet.WriteDouble(Server.RateMobEXP);
            packet.WriteDouble(Server.RateMesoAmount);
            packet.WriteDouble(Server.RateDropChance);
            SendPacket(packet);

            var e = World.RunningEvent;
            if (e != null && string.IsNullOrEmpty(e.ScrollingHeader) == false)
            {
                var p = new Packet(ISServerMessages.WSE_ChangeScrollingHeader);
                p.WriteString(e.ScrollingHeader);
                SendPacket(p);
            }
        }

        public void SendUserNoUpdateToLogins()
        {

            var packet = new Packet(ISServerMessages.ServerSetUserNo);

            // Should be initialized from loginserver
            var world = World;

            for (byte i = 0; i < world.Channels; i++)
            {
                if (world.GameServers.TryGetValue(i, out LocalServer game))
                {
                    packet.WriteInt(game.Connections);
                }
                else
                {
                    packet.WriteInt(0);
                }
            }

            foreach (var kvp in CenterServer.Instance.LocalServers.Where(x => x.Value.Type == LocalServerType.Login))
            {
                kvp.Value.Connection?.SendPacket(packet);
            }
        }

        public override void OnDisconnect()
        {
            if (Server != null)
            {
                Program.MainForm.LogAppend($"Server disconnected: {Server.Name}");

                Server.SetConnection(null);

                Server = null;
            }
            Pinger.Remove(this);
        }

        public override void AC_OnPacketInbound(Packet packet)
        {
            try
            {
                if (Server == null)
                {
                    switch ((ISClientMessages)packet.ReadByte())
                    {
                        case ISClientMessages.ServerRequestAllocation:
                            {
                                string serverName = packet.ReadString();
                                LocalServer ls;
                                if (!CenterServer.Instance.LocalServers.TryGetValue(serverName, out ls))
                                {
                                    Program.MainForm.LogAppend("Server doesn't exist in configuration: " + serverName + ". Disconnecting.");
                                    Disconnect();
                                    return;
                                }
                                var publicIp = System.Net.IPAddress.Parse(packet.ReadString());
                                var port = packet.ReadUShort();

                                Program.MainForm.LogAppend(
                                    $"Server connecting... Name: {serverName}, Public IP: {publicIp}, Port {port}");

                                if (ls.Type == LocalServerType.Game || ls.Type == LocalServerType.Shop)
                                {
                                    byte worldid = packet.ReadByte();
                                    if (World.ID != worldid)
                                    {
                                        Program.MainForm.LogAppend(
                                            $"{serverName} disconnected because it didn't have a valid world ID ({worldid})");
                                        Disconnect();
                                        return;
                                    }
                                }


                                if (ls.Connected)
                                {
                                    if (ls.InMaintenance)
                                    {
                                        Program.MainForm.LogAppend(
                                            $"Server is already connected: {serverName}, but already in maintenance. Disconnecting.");
                                        Disconnect();
                                        return;
                                    }

                                    Program.MainForm.LogAppend(
                                        $"Server is already connected: {serverName}. Setting up transfer...");
                                    ls.InMaintenance = true;
                                }

                                Server = ls;
                                Server.PublicIP = publicIp;
                                Server.Port = port;
                                Server.SetConnection(this);

                                Packet pw = new Packet(ISServerMessages.ServerAssignmentResult);
                                pw.WriteBool(Server.InMaintenance);

                                if (ls.Type == LocalServerType.Game || ls.Type == LocalServerType.Shop)
                                {
                                    pw.WriteByte(Server.ChannelID);
                                }

                                if (Server.Type == LocalServerType.Game)
                                {
                                    Program.MainForm.LogAppend(
                                        $"Gameserver assigned! Name {serverName}; Channel ID {Server.ChannelID}");
                                }
                                else if (Server.Type == LocalServerType.Login)
                                {
                                    Program.MainForm.LogAppend("Login connected.");
                                }
                                else if (Server.Type == LocalServerType.Shop)
                                {
                                    Program.MainForm.LogAppend($"Shopserver assigned on idx {Server.ChannelID}");
                                }

                                SendPacket(pw);

                                SendRates();


                                break;
                            }
                    }
                }
                else
                {
                    var opcode = (ISClientMessages)packet.ReadByte();
                    switch (opcode)
                    {
                        case ISClientMessages.ServerMigrationUpdate:
                            {
                                if (!Server.InMaintenance)
                                {
                                    Program.MainForm.LogAppend("Received ServerMigrationUpdate while not in maintenance!");
                                    break;
                                }

                                var forwardPacket = new Packet(ISServerMessages.ServerMigrationUpdate);
                                forwardPacket.WriteBytes(packet.ReadLeftoverBytes());

                                // Figure out what way we need to send the packet
                                if (Server.Connection == this)
                                    Server.TransferConnection.SendPacket(forwardPacket);
                                else
                                    Server.Connection.SendPacket(forwardPacket);

                                break;
                            }

                        case ISClientMessages.ChangeRates:
                            {
                                Server.RateMobEXP = packet.ReadDouble();
                                Server.RateMesoAmount = packet.ReadDouble();
                                Server.RateDropChance = packet.ReadDouble();
                                break;
                            }
                        case ISClientMessages.ServerSetConnectionsValue:
                            {
                                Server.Connections = packet.ReadInt();
                                break;
                            }
                        case ISClientMessages.PlayerChangeServer:
                            {
                                string hash = packet.ReadString();
                                int charid = packet.ReadInt();
                                byte world = packet.ReadByte();
                                byte channel = packet.ReadByte();
                                bool CCing = packet.ReadBool();

                                var chr = CenterServer.Instance.FindCharacter(charid);

                                Packet pw = new Packet(ISServerMessages.PlayerChangeServerResult);
                                pw.WriteString(hash);
                                pw.WriteInt(charid);

                                bool found = true;
                                LocalServer ls = null;
                                // this will null the key, so if there were two instances CCing,
                                // both would probably get killed.
                                if (RedisBackend.Instance.PlayerIsMigrating(charid, false))
                                {
                                    Program.MainForm.LogAppend("Character {0} tried to CC while already CCing.", charid);
                                    pw.WriteInt(0);
                                    pw.WriteShort(0);
                                    found = false;
                                }
                                else if (channel < 50 &&
                                    World.GameServers.TryGetValue(channel, out ls) &&
                                    ls.Connected)
                                {
                                    pw.WriteBytes(ls.PublicIP.GetAddressBytes());
                                    pw.WriteUShort(ls.Port);

                                    RedisBackend.Instance.SetMigratingPlayer(charid);

                                    if (chr != null)
                                    {
                                        chr.isCCing = true;
                                    }

                                    if (this.Server.Type == LocalServerType.Login)
                                    {
                                        CharacterDBAccessor.UpdateRank(charid);
                                    }
                                }
                                else if (channel >= 50 &&
                                    World.ShopServers.TryGetValue((byte)(channel - 50), out ls) &&
                                    ls.Connected)
                                {
                                    pw.WriteBytes(ls.PublicIP.GetAddressBytes());
                                    pw.WriteUShort(ls.Port);

                                    RedisBackend.Instance.SetMigratingPlayer(charid);

                                    if (chr != null)
                                    {
                                        chr.isCCing = true;
                                        chr.LastChannel = chr.ChannelID;
                                        chr.InCashShop = true;
                                    }

                                }
                                else
                                {
                                    Program.MainForm.LogAppend("Character {0} tried to CC to channel that is not online.", charid);
                                    pw.WriteInt(0);
                                    pw.WriteShort(0);
                                    found = false;
                                }


                                if (CCing && found && chr != null && ls != null)
                                {
                                    chr.FriendsList.SaveBuddiesToDb();
                                    chr.isConnectingFromLogin = false;

                                    // Give the channel server some info from this server
                                    var channelPacket = new Packet(ISServerMessages.PlayerChangeServerData);
                                    channelPacket.WriteInt(charid);
                                    channelPacket.WriteBytes(packet.ReadLeftoverBytes());

                                    if (Server.ChannelID == channel &&
                                        Server.InMaintenance)
                                    {
                                        // Server in maintenance...
                                        ls.TransferConnection?.SendPacket(channelPacket);
                                    }
                                    else
                                    {
                                        // Changing channels, meh
                                        ls.Connection?.SendPacket(channelPacket);
                                    }
                                }

                                SendPacket(pw);
                                break;
                            }

                        case ISClientMessages.ServerRegisterUnregisterPlayer: // Register/unregister character
                            {
                                int charid = packet.ReadInt();
                                bool add = packet.ReadBool();
                                if (add)
                                {
                                    string charname = packet.ReadString();
                                    short job = packet.ReadShort();
                                    byte level = packet.ReadByte();
                                    byte admin = packet.ReadByte();
                                    var character = CenterServer.Instance.AddCharacter(charname, charid, Server.ChannelID, job, level, admin);

                                    if (Party.Parties.TryGetValue(character.PartyID, out Party party))
                                    {
                                        party.SilentUpdate(character.ID);
                                    }
                                    else if (character.PartyID != 0)
                                    {
                                        Program.MainForm.LogAppend("Trying to register a character, but the party was not found??? PartyID: {0}, character ID {1}", character.PartyID, charid);
                                        character.PartyID = 0;
                                    }

                                    var friendsList = character.FriendsList;
                                    if (friendsList != null)
                                    {
                                        friendsList.OnOnlineCC(true, false);
                                        friendsList.SendBuddyList();
                                        friendsList.PollRequests();
                                    }
                                }
                                else
                                {
                                    bool ccing = packet.ReadBool();
                                    var character = CenterServer.Instance.FindCharacter(charid);
                                    if (ccing == false)
                                    {
                                        character.IsOnline = false;

                                        if (Party.Parties.TryGetValue(character.PartyID, out Party party))
                                        {
                                            if (party.leader.id == charid)
                                            {
                                                // Disband the party
                                                party.Leave(character);
                                            }
                                            else
                                            {
                                                party.SilentUpdate(character.ID);
                                            }
                                        }

                                        character.FriendsList?.OnOnlineCC(true, true);

                                        // Fix this. When you log back in, the chat has 2 of you.
                                        // Messenger.LeaveMessenger(character.ID);
                                    }

                                    if (Party.Invites.ContainsKey(character.ID)) Party.Invites.Remove(character.ID);
                                }

                                SendUserNoUpdateToLogins();

                                break;
                            }


                        case ISClientMessages.BroadcastPacketToGameservers:
                            {
                                var p = new Packet(packet.ReadLeftoverBytes());
                                World.SendPacketToEveryGameserver(p);
                                break;
                            }

                        case ISClientMessages.BroadcastPacketToShopservers:
                            {
                                var p = new Packet(packet.ReadLeftoverBytes());
                                World.SendPacketToEveryShopserver(p);
                                break;
                            }

                        default:
                            switch (Server.Type)
                            {
                                case LocalServerType.Game: HandleGamePacket(opcode, packet); break;
                                case LocalServerType.Login: HandleLoginPacket(opcode, packet); break;
                                case LocalServerType.Shop: HandleShopPacket(opcode, packet); break;
                            }

                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Program.LogFile.WriteLine("Exception Caught:\r\n{0}", ex.ToString());
                //FileWriter.WriteLine(@"etclog\ExceptionCatcher.log", "[Center Server][" + DateTime.Now.ToString() + "] Exception caught: " + ex.Message + Environment.NewLine + Environment.NewLine + "Stacktrace: " + ex.StackTrace, true);
                //Disconnect();
            }
        }

        private void HandleLoginPacket(ISClientMessages opcode, Packet packet)
        {
            switch (opcode)
            {
                case ISClientMessages.PlayerRequestWorldLoad:
                    {
                        string hash = packet.ReadString();
                        byte world = packet.ReadByte();

                        Packet pw = new Packet(ISServerMessages.PlayerRequestWorldLoadResult);
                        pw.WriteString(hash);

                        if (World.ID == world)
                        {
                            World.AddWarning(pw);
                        }
                        else
                        {
                            pw.WriteByte(2); // full load
                        }

                        SendPacket(pw);
                        break;
                    }
                case ISClientMessages.PlayerRequestChannelStatus: // channel online check
                    {
                        string hash = packet.ReadString();
                        byte world = packet.ReadByte();
                        byte channel = packet.ReadByte();
                        int accountId = packet.ReadInt();

                        Packet pw = new Packet(ISServerMessages.PlayerRequestChannelStatusResult);
                        pw.WriteString(hash);

                        if (World.ID != world ||
                            World.GameServers.TryGetValue(channel, out LocalServer ls) == false ||
                            ls.InMaintenance ||
                            !ls.Connected)
                        {
                            pw.WriteByte(0x09); // Channel Offline
                        }
                        else
                        {
                            pw.WriteByte(0);
                            pw.WriteByte(channel);

                            try
                            {
                                var ids = CharacterDBAccessor.GetCharacterIdList(accountId).ToList();
                                pw.WriteByte((byte)ids.Count);

                                foreach (var id in ids)
                                {
                                    var ad = CharacterDBAccessor.LoadAvatar(id);
                                    var ranking = CharacterDBAccessor.LoadRank(id);

                                    ad.Encode(pw);
                                    pw.WriteBool(ranking != null);
                                    if (ranking != null)
                                    {
                                        var (worldRank, worldRankMove, jobRank, jobRankMove) = ranking.Value;
                                        pw.WriteInt(worldRank);
                                        pw.WriteInt(worldRankMove);
                                        pw.WriteInt(jobRank);
                                        pw.WriteInt(jobRankMove);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Program.MainForm.LogAppend("Error while building packet for characterselect! {0}", ex);
                                _log.Error(ex);
                                pw = new Packet(ISServerMessages.PlayerRequestChannelStatusResult);
                                pw.WriteString(hash);
                                pw.WriteByte(1);
                            }
                        }

                        SendPacket(pw);
                        break;
                    }

                case ISClientMessages.PlayerDeleteCharacter:
                    {
                        string hash = packet.ReadString();
                        int accountId = packet.ReadInt();
                        int charId = packet.ReadInt();

                        var p = new Packet(ISServerMessages.PlayerDeleteCharacterResult);
                        p.WriteString(hash);
                        p.WriteInt(charId);
                        try
                        {
                            var deleteCharacterResult = CharacterDBAccessor.DeleteCharacter(accountId, charId);
                            p.WriteByte(deleteCharacterResult);

                            if (deleteCharacterResult == 0)
                            {
                                var foundChar = CenterServer.Instance.FindCharacter(charId, false);
                                if (foundChar != null)
                                {
                                    if (foundChar.PartyID != 0 &&
                                        Party.Parties.TryGetValue(foundChar.PartyID, out Party party))
                                    {
                                        party.Leave(foundChar);
                                    }

                                    // Registered, so get rid of it
                                    CenterServer.Instance.CharacterStore.Remove(foundChar);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex);
                            Program.MainForm.LogAppend("Error while deleting character! {0}", ex);
                            p.WriteByte(10);
                        }
                        SendPacket(p);
                        break;
                    }

                case ISClientMessages.PlayerCreateCharacterNamecheck:
                    {
                        string hash = packet.ReadString();
                        string charname = packet.ReadString();

                        var p = new Packet(ISServerMessages.PlayerCreateCharacterNamecheckResult);
                        p.WriteString(hash);
                        p.WriteString(charname);
                        try
                        {
                            p.WriteBool(CharacterDBAccessor.CheckDuplicateID(charname));
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex);
                            Program.MainForm.LogAppend("Error while checking for duplicate ID! {0}", ex);
                            p.WriteBool(true);
                        }

                        SendPacket(p);
                        break;
                    }

                case ISClientMessages.PlayerCreateCharacter:
                    {
                        string hash = packet.ReadString();
                        int accountId = packet.ReadInt();
                        byte gender = packet.ReadByte();

                        string charname = packet.ReadString();

                        int face = packet.ReadInt();
                        int hair = packet.ReadInt();
                        int haircolor = packet.ReadInt();
                        int skin = packet.ReadInt();

                        int top = packet.ReadInt();
                        int bottom = packet.ReadInt();
                        int shoes = packet.ReadInt();
                        int weapon = packet.ReadInt();

                        byte str = packet.ReadByte();
                        byte dex = packet.ReadByte();
                        byte intt = packet.ReadByte();
                        byte luk = packet.ReadByte();

                        var p = new Packet(ISServerMessages.PlayerCreateCharacterResult);
                        p.WriteString(hash);

                        try
                        {
                            if (CharacterDBAccessor.CheckDuplicateID(charname))
                            {
                                p.WriteBool(false);
                            }
                            else
                            {
                                int id = CharacterDBAccessor.CreateNewCharacter(
                                    accountId,
                                    charname,
                                    gender,

                                    face, hair, haircolor, skin,
                                    str, dex, intt, luk,
                                    top, bottom, shoes, weapon
                                );

                                var ad = CharacterDBAccessor.LoadAvatar(id);

                                p.WriteBool(true);

                                ad.Encode(p);
                            }
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex);
                            Program.MainForm.LogAppend("Error while creating character! {0}", ex);

                            p = new Packet(ISServerMessages.PlayerCreateCharacterResult);
                            p.WriteString(hash);
                            p.WriteBool(false);
                        }

                        SendPacket(p);

                        break;
                    }
            }
        }

        private void HandleGamePacket(ISClientMessages opcode, Packet packet)
        {
            switch (opcode)
            {

                #region Messenger


                case ISClientMessages.MessengerJoin:
                    Messenger.JoinMessenger(packet);
                    break;

                case ISClientMessages.MessengerLeave:
                    Messenger.LeaveMessenger(packet.ReadInt());
                    break;

                case ISClientMessages.MessengerInvite:
                    Messenger.SendInvite(packet.ReadInt(), packet.ReadString());
                    break;
                case ISClientMessages.MessengerBlocked:
                    Messenger.Block(packet);
                    break;
                case ISClientMessages.MessengerAvatar:
                    Messenger.OnAvatar(packet);
                    break;
                case ISClientMessages.MessengerChat:
                    Messenger.Chat(packet.ReadInt(), packet.ReadString());
                    break;

                #endregion

                #region Party

                case ISClientMessages.PartyCreate:
                    {
                        int fuker = packet.ReadInt();
                        Character fucker = CenterServer.Instance.FindCharacter(fuker);
                        Party.CreateParty(fucker);
                        break;
                    }

                case ISClientMessages.PartyInvite:
                    {
                        int fuker1 = packet.ReadInt();
                        int fuker2 = packet.ReadInt();
                        Character fucker1 = CenterServer.Instance.FindCharacter(fuker1);
                        if (fucker1 != null && Party.Parties.TryGetValue(fucker1.PartyID, out Party party))
                        {
                            party.Invite(fuker1, fuker2);
                        }
                        break;
                    }

                case ISClientMessages.PartyAccept:
                    {
                        int AcceptorID = packet.ReadInt();
                        Character fucker1 = CenterServer.Instance.FindCharacter(AcceptorID);

                        if (fucker1 != null && Party.Invites.TryGetValue(AcceptorID, out Party party))
                        {
                            party.TryJoin(fucker1);
                        }

                        break;
                    }

                case ISClientMessages.PartyLeave:
                    {
                        int LeaverID = packet.ReadInt();
                        Character fucker = CenterServer.Instance.FindCharacter(LeaverID);

                        if (fucker != null && Party.Parties.TryGetValue(fucker.PartyID, out Party party))
                        {
                            party.Leave(fucker);
                        }

                        break;
                    }

                case ISClientMessages.PartyExpel:
                    {
                        int leader = packet.ReadInt();
                        int expelledCharacter = packet.ReadInt();
                        Character fucker = CenterServer.Instance.FindCharacter(leader);
                        if (fucker != null && Party.Parties.TryGetValue(fucker.PartyID, out Party party))
                        {
                            party.Expel(leader, expelledCharacter);
                        }

                        break;
                    }

                case ISClientMessages.PartyDecline:
                    {
                        int decliner = packet.ReadInt();
                        String declinerName = packet.ReadString();
                        Character chr = CenterServer.Instance.FindCharacter(decliner);
                        if (chr != null && Party.Invites.TryGetValue(decliner, out Party party))
                        {
                            party.DeclineInvite(chr);
                        }
                        break;
                    }

                case ISClientMessages.PartyChat:
                    {
                        int chatter = packet.ReadInt();
                        string msg = packet.ReadString();
                        Character chr = CenterServer.Instance.FindCharacter(chatter);
                        if (chr != null && Party.Parties.TryGetValue(chr.PartyID, out Party party))
                        {
                            party.Chat(chatter, msg);
                        }
                        break;
                    }

                case ISClientMessages.PlayerUpdateMap:
                    {
                        int id = packet.ReadInt();
                        int map = packet.ReadInt();
                        Character fucker = CenterServer.Instance.FindCharacter(id);

                        if (fucker != null)
                        {
                            fucker.MapID = map;
                            if (Party.Parties.TryGetValue(fucker.PartyID, out Party party))
                            {
                                party.SilentUpdate(id);
                            }
                        }

                        break;
                    }

                case ISClientMessages.PartyDoorChanged:
                    {
                        int chrid = packet.ReadInt();
                        var door = new DoorInformation(packet.ReadInt(), packet.ReadInt(), packet.ReadShort(), packet.ReadShort(), chrid);

                        var chr = CenterServer.Instance.FindCharacter(chrid);
                        if (chr != null && Party.Parties.TryGetValue(chr.PartyID, out var party))
                        {
                            party.UpdateDoor(door, chrid);
                        }

                        break;
                    }
                #endregion

                #region Buddy

                case ISClientMessages.BuddyInvite:
                    {
                        int inviterId = packet.ReadInt();
                        String inviterName = packet.ReadString();
                        String toInviteName = packet.ReadString();

                        Character inviter = CenterServer.Instance.FindCharacter(inviterName);
                        Character toInvite = CenterServer.Instance.FindCharacter(toInviteName, false);
                        if (inviter == null) return;

                        if (inviter.FriendsList.IsFull())
                        {
                            inviter.FriendsList.SendRequestError(12);
                            return;
                        }

                        if (toInvite == null)
                        {
                            //How to get id from name? O.o
                            try
                            {
                                var namedata = CenterServer.Instance.CharacterDatabase.RunQuery("SELECT c.`ID`, u.admin, c.buddylist_size, (SELECT COUNT(*) FROM buddylist WHERE charid = c.ID) AS `current_buddylist_size` FROM characters c JOIN users u ON u.id = c.userid WHERE c.name = @name", "@name", toInviteName) as MySqlDataReader;
                                if (namedata.Read())
                                {
                                    int invitedid = namedata.GetInt32("ID");
                                    int maxBuddyListSize = namedata.GetInt32("buddylist_size");
                                    int buddyListSize = namedata.GetInt32("current_buddylist_size");
                                    bool isGM = namedata.GetByte("admin") > 0;
                                    namedata.Close();

                                    if (isGM && inviter.IsGM == false)
                                    {
                                        inviter.FriendsList.SendRequestError(14);
                                        return;
                                    }

                                    if (maxBuddyListSize <= buddyListSize)
                                    {
                                        // buddylist is full
                                        inviter.FriendsList.SendRequestError(12);
                                        return;
                                    }

                                    inviter.FriendsList.Add(new BuddyData(invitedid, toInviteName));
                                    // No update?

                                    CenterServer.Instance.CharacterDatabase.RunQuery(
                                        "DELETE FROM buddylist_pending WHERE charid = @toinviteid AND inviter_charid = @inviterid",
                                        "@toinviteid", invitedid,
                                        "@inviterid", inviterId
                                    );

                                    CenterServer.Instance.CharacterDatabase.RunQuery(
                                        "INSERT INTO buddylist_pending (charid, inviter_charid, inviter_charname) VALUES (@toinviteid, @inviterid, @invitername)",
                                        "@toinviteid", invitedid,
                                        "@inviterid", inviterId,
                                        "@invitername", inviterName
                                    );
                                }
                                else
                                {
                                    namedata.Close();
                                    inviter.FriendsList.SendRequestError(15);
                                }
                            }
                            catch (Exception e)
                            {
                                BuddyList.log.Error($"Offline buddy request failed for {inviterId} {inviterName} {toInviteName}", e);
                            }
                        }
                        else
                        {
                            toInvite.FriendsList.Request(inviter.FriendsList.Owner);
                        }
                        break;
                    }
                case ISClientMessages.BuddyUpdate:
                    {
                        int id = packet.ReadInt();
                        string name = packet.ReadString();
                        Character toUpdate = CenterServer.Instance.FindCharacter(id);
                        toUpdate.FriendsList.OnOnlineCC(true, false);
                        break;
                    }
                case ISClientMessages.BuddyInviteAnswer:
                    {
                        int id = packet.ReadInt();
                        String name = packet.ReadString();
                        Character toAccept = CenterServer.Instance.FindCharacter(id);
                        toAccept.FriendsList.AcceptRequest();
                        break;
                    }
                case ISClientMessages.BuddyListExpand:
                    {
                        CenterServer.Instance.FindCharacter(packet.ReadInt()).FriendsList.IncreaseCapacity();
                        break;
                    }
                case ISClientMessages.BuddyChat:
                    {
                        int fWho = packet.ReadInt();
                        string Who = packet.ReadString();
                        string what = packet.ReadString();
                        int recipientCount = packet.ReadByte();
                        int[] recipients = new int[recipientCount];
                        for (var i = 0; i < recipientCount; i++) recipients[i] = packet.ReadInt();

                        Character pWho = CenterServer.Instance.FindCharacter(fWho);

                        pWho?.FriendsList.BuddyChat(what, recipients);
                        break;
                    }
                case ISClientMessages.BuddyDecline:
                    {
                        Character Who = CenterServer.Instance.FindCharacter(packet.ReadInt());
                        int victimId = packet.ReadInt();
                        Character Victim = CenterServer.Instance.FindCharacter(victimId);
                        Who.FriendsList.RemoveBuddyOrRequest(Victim, victimId);
                        break;
                    }
                #endregion

                case ISClientMessages.PlayerUsingSuperMegaphone:
                    {
                        Packet pw = new Packet(ISServerMessages.PlayerSuperMegaphone);
                        pw.WriteString(packet.ReadString());
                        pw.WriteBool(packet.ReadBool());
                        pw.WriteByte(packet.ReadByte());
                        World.SendPacketToEveryGameserver(pw);
                        break;
                    }

                case ISClientMessages.PlayerWhisperOrFindOperation: // WhisperOrFind
                    {
                        int sender = packet.ReadInt();
                        Character senderChar = CenterServer.Instance.FindCharacter(sender);
                        if (senderChar == null)
                            return;

                        bool whisper = packet.ReadBool();
                        string receiver = packet.ReadString();
                        Character receiverChar = CenterServer.Instance.FindCharacter(receiver);

                        if (whisper)
                        {
                            string message = packet.ReadString();
                            if ((receiverChar == null ||
                                !World.GameServers.ContainsKey(receiverChar.ChannelID)) ||
                                (receiverChar.IsGM && !senderChar.IsGM))
                            {
                                Packet pw = new Packet(ISServerMessages.PlayerWhisperOrFindOperationResult);
                                pw.WriteBool(true); // Whisper
                                pw.WriteBool(false); // Not found.
                                pw.WriteInt(sender);
                                pw.WriteString(receiver);
                                SendPacket(pw);
                            }
                            else
                            {
                                Packet pw = new Packet(ISServerMessages.PlayerWhisperOrFindOperationResult);
                                pw.WriteBool(false); // Find
                                pw.WriteBool(true); // Found.
                                pw.WriteInt(sender);
                                pw.WriteString(receiver);
                                pw.WriteSByte(-1);
                                pw.WriteSByte(-1);
                                SendPacket(pw);

                                pw = new Packet(ISServerMessages.PlayerWhisperOrFindOperationResult);
                                pw.WriteBool(true); // Whisper
                                pw.WriteBool(true); // Found.
                                pw.WriteInt(receiverChar.ID);
                                pw.WriteString(senderChar.Name);
                                pw.WriteByte(senderChar.ChannelID);
                                pw.WriteString(message);
                                pw.WriteBool(false); // false is '>>'
                                LocalServer victimChannel = World.GameServers[receiverChar.ChannelID];
                                victimChannel.Connection.SendPacket(pw);
                            }
                        }
                        else
                        {
                            if (receiverChar == null ||
                                !World.GameServers.ContainsKey(receiverChar.ChannelID) ||
                                (receiverChar.IsGM && !senderChar.IsGM))
                            {
                                Packet pw = new Packet(ISServerMessages.PlayerWhisperOrFindOperationResult);
                                pw.WriteBool(false); // Find
                                pw.WriteBool(false); // Not found.
                                pw.WriteInt(sender);
                                pw.WriteString(receiver);
                                SendPacket(pw);
                            }
                            else
                            {
                                Packet pw = new Packet(ISServerMessages.PlayerWhisperOrFindOperationResult);
                                pw.WriteBool(false); // Find
                                pw.WriteBool(true); // Found.
                                pw.WriteInt(senderChar.ID);
                                pw.WriteString(receiverChar.Name);
                                // Cashshop
                                if (receiverChar.InCashShop)
                                    pw.WriteSByte(-2);
                                else
                                    pw.WriteByte(receiverChar.ChannelID);
                                pw.WriteSByte(0);
                                SendPacket(pw);
                            }

                        }
                        break;
                    }

                case ISClientMessages.UpdatePlayerJobLevel:
                    {
                        int charId = packet.ReadInt();
                        var character = CenterServer.Instance.FindCharacter(charId);
                        if (character == null)
                            return;

                        character.Job = packet.ReadShort();
                        character.Level = packet.ReadByte();
                        break;
                    }


                case ISClientMessages.AdminMessage:
                    {
                        Packet pw = new Packet(ISServerMessages.AdminMessage);
                        pw.WriteString(packet.ReadString());
                        pw.WriteByte(packet.ReadByte());
                        World.SendPacketToEveryGameserver(pw);
                        break;
                    }

                case ISClientMessages.KickPlayer:
                    {
                        int userId = packet.ReadInt();
                        Program.MainForm.LogAppend("Globally kicking user " + userId);
                        Packet pw = new Packet(ISServerMessages.KickPlayerResult);
                        pw.WriteInt(userId);
                        World.SendPacketToEveryGameserver(pw);
                        break;
                    }

                case ISClientMessages.ReloadEvents:
                    CenterServer.Instance.ReloadEvents();
                    break;
            }
        }

        private void HandleShopPacket(ISClientMessages opcode, Packet packet)
        {
            switch (opcode)
            {
                case ISClientMessages.PlayerQuitCashShop: // CC back to channel from cashserver
                    {
                        string hash = packet.ReadString();
                        int charid = packet.ReadInt();
                        byte world = packet.ReadByte();
                        Character chr = CenterServer.Instance.FindCharacter(charid);
                        if (chr == null) return;

                        Packet pw = new Packet(ISServerMessages.PlayerChangeServerResult);
                        pw.WriteString(hash);
                        pw.WriteInt(charid);

                        if (World.ID == world &&
                            World.GameServers.TryGetValue(chr.LastChannel, out LocalServer ls))
                        {
                            pw.WriteBytes(ls.PublicIP.GetAddressBytes());
                            pw.WriteUShort(ls.Port);

                            RedisBackend.Instance.SetMigratingPlayer(charid);

                            chr.InCashShop = false;
                            chr.isCCing = true;
                            chr.LastChannel = 0;

                            // Give the channel server some info from this server
                            var channelPacket = new Packet(ISServerMessages.PlayerChangeServerData);
                            channelPacket.WriteInt(charid);
                            channelPacket.WriteBytes(packet.ReadLeftoverBytes());

                            if (Server.InMaintenance)
                            {
                                // Server in maintenance...
                                ls.TransferConnection?.SendPacket(channelPacket);
                            }
                            else
                            {
                                // Changing channels, meh
                                ls.Connection?.SendPacket(channelPacket);
                            }
                        }
                        else
                        {
                            pw.WriteInt(0);
                            pw.WriteShort(0);
                        }
                        SendPacket(pw);

                        break;
                    }

            }
        }
    }

}