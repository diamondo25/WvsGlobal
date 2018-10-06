using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Net;
using log4net;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;
using WvsBeta.Game.Events.PartyQuests;

namespace WvsBeta.Game
{
    public partial class CenterSocket : AbstractConnection
    {
        private bool disconnectExpected;
        public CenterSocket()
            : base(Server.Instance.CenterIP.ToString(), (ushort)Server.Instance.CenterPort)
        {

        }

        public override void OnDisconnect()
        {
            Server.Instance.CenterConnection = null;
            if (disconnectExpected)
            {
                Server.Instance.ConnectToCenter();
            }
            else
            {
                Program.MainForm.LogAppend("Disconnected from the Center Server! Something went wrong! :S");
                // release all connections
                Program.MainForm.Shutdown();
            }
        }

        public override void OnHandshakeInbound(Packet pPacket)
        {
            Packet packet2 = new Packet(ISClientMessages.ServerRequestAllocation);
            packet2.WriteString(Server.Instance.Name);
            packet2.WriteString(Server.Instance.PublicIP.ToString());
            packet2.WriteUShort(Server.Instance.Port);
            packet2.WriteByte(Server.Instance.WorldID);
            packet2.WriteString(Server.Instance.WorldName);
            SendPacket(packet2);
        }

        public override void AC_OnPacketInbound(Packet packet)
        {
            ISServerMessages msg = (ISServerMessages)packet.ReadByte();
            ////Console.WriteLine("received centerserver message: " + msg);
            try
            {
                switch (msg)
                {
                    case ISServerMessages.Pong:
                    case ISServerMessages.Ping: break;

                    case ISServerMessages.ChangeCenterServer:
                        {
                            var ip = packet.ReadBytes(4);
                            var port = packet.ReadUShort();
                            disconnectExpected = true;
                            Server.Instance.CenterIP = new IPAddress(ip);
                            Server.Instance.CenterPort = port;
                            Server.Instance.CenterMigration = true;
                            Disconnect();
                            break;
                        }

                    case ISServerMessages.ChangeRates:
                        {
                            double mobexprate = packet.ReadDouble();
                            double mesosamountrate = packet.ReadDouble();
                            double dropchancerate = packet.ReadDouble();

                            if (mobexprate > 0 && mobexprate != Server.Instance.RateMobEXP)
                            {
                                Server.Instance.RateMobEXP = mobexprate;
                                Program.MainForm.LogAppend("Changed EXP Rate to {0}", mobexprate);
                            }
                            if (mesosamountrate > 0 && mesosamountrate != Server.Instance.RateMesoAmount)
                            {
                                Server.Instance.RateMesoAmount = mesosamountrate;
                                Program.MainForm.LogAppend("Changed Meso Rate to {0}", mesosamountrate);
                            }
                            if (dropchancerate > 0 && dropchancerate != Server.Instance.RateDropChance)
                            {
                                Server.Instance.RateDropChance = dropchancerate;
                                Program.MainForm.LogAppend("Changed Drop Rate to {0}", dropchancerate);
                            }

                            var currentDateTime = MasterThread.CurrentDate;
                            Server.Instance.CharacterList.ForEach(x => x.Value?.SetIncExpRate(currentDateTime.Day, currentDateTime.Hour));

                            SendUpdateRates();
                            break;
                        }

                    case ISServerMessages.WSE_ChangeScrollingHeader:
                        {
                            var str = packet.ReadString();
                            var newIsEmpty = string.IsNullOrEmpty(str);
                            var oldIsEmpty = string.IsNullOrEmpty(Server.Instance.ScrollingHeader);

                            // Do not update if there's already a message running 
                            if ((newIsEmpty && !oldIsEmpty) ||
                                (!newIsEmpty && oldIsEmpty))
                            {
                                Server.Instance.SetScrollingHeader(str);
                            }

                            break;
                        }

                    case ISServerMessages.ReloadNPCScript:
                        {
                            var scriptName = packet.ReadString();

                            Program.MainForm.LogAppend("Processing reload npc script request... Script: " + scriptName);

                            Server.Instance.ForceCompileScriptfile(
                                Server.Instance.GetScriptFilename(scriptName),
                                null
                            );
                            break;
                        }

                    case ISServerMessages.ServerAssignmentResult:
                        {
                            var inMigration = Server.Instance.InMigration = packet.ReadBool();
                            Server.Instance.ID = packet.ReadByte();

                            GlobalContext.Properties["ChannelID"] = Server.Instance.ID;

                            if (inMigration)
                            {
                                Program.MainForm.LogAppend("Server Migration in process...");
                                Server.Instance.IsNewServerInMigration = true;
                            }
                            else if (!Server.Instance.CenterMigration)
                            {
                                Server.Instance.StartListening();

                                Program.MainForm.LogAppend($"Handling as Game Server {Server.Instance.ID} on World {Server.Instance.WorldID} ({Server.Instance.WorldName})");
                            }
                            else
                            {
                                Program.MainForm.LogAppend("Reconnected to center server?");
                            }

                            Server.Instance.CenterMigration = false;
                            break;
                        }

                    case ISServerMessages.ServerMigrationUpdate:
                        {

                            var pw = new Packet(ISClientMessages.ServerMigrationUpdate);
                            switch ((ServerMigrationStatus)packet.ReadByte())
                            {
                                case ServerMigrationStatus.StartListening:
                                    {
                                        Server.Instance.StartListening();
                                        pw.WriteByte((byte)ServerMigrationStatus.DataTransferRequest);
                                        SendPacket(pw);
                                        break;
                                    }
                                case ServerMigrationStatus.DataTransferRequest:
                                    {
                                        pw.WriteByte((byte)ServerMigrationStatus.DataTransferResponse);

                                        using (var uncompressedPacket = new Packet())
                                        {
                                            var mapsWithDrops =
                                                DataProvider.Maps.Where(x => x.Value.DropPool.Drops.Count > 0)
                                                    .ToArray();
                                            uncompressedPacket.WriteInt(mapsWithDrops.Length);
                                            foreach (var map in mapsWithDrops)
                                            {
                                                uncompressedPacket.WriteInt(map.Key);
                                                map.Value.DropPool.EncodeForMigration(uncompressedPacket);
                                            }

                                            PartyData.EncodeForTransfer(uncompressedPacket);

                                            uncompressedPacket.GzipCompress(pw);

                                            Program.MainForm.LogAppend("Sent " + mapsWithDrops.Length + " map updates... (packet size: " + pw.Length + " bytes)");
                                        }

                                        SendPacket(pw);
                                        break;
                                    }
                                case ServerMigrationStatus.DataTransferResponse:
                                    {

                                        using (var gzipStream = new GZipStream(packet.MemoryStream, CompressionMode.Decompress))
                                        using (var decompressedPacket = new Packet(gzipStream))
                                        {
                                            var maps = decompressedPacket.ReadInt();
                                            for (var i = 0; i < maps; i++)
                                            {
                                                var mapid = decompressedPacket.ReadInt();
                                                DataProvider.Maps[mapid].DropPool
                                                    .DecodeForMigration(decompressedPacket);
                                            }

                                            if (decompressedPacket.Length != decompressedPacket.Position)
                                            {
                                                PartyData.DecodeForTransfer(decompressedPacket);
                                            }

                                            Program.MainForm.LogAppend("Updated " + maps + " maps...");
                                        }

                                        pw.WriteByte((byte)ServerMigrationStatus.FinishedInitialization);
                                        SendPacket(pw);
                                        break;
                                    }

                                case ServerMigrationStatus.FinishedInitialization:
                                    {
                                        Program.MainForm.LogAppend("Other side is ready, start CC. Connections: " + Pinger.CurrentLoggingConnections);

                                        int timeout = 15;

                                        var startTime = MasterThread.CurrentTime;
                                        bool sentPacket = false;
                                        bool disconnectedPlayers = false;
                                        MasterThread.RepeatingAction.Start(
                                            "Client Migration Thread",
                                            date =>
                                            {
                                                if (Pinger.CurrentLoggingConnections == 0 ||
                                                    (date - startTime) > timeout * 1000)
                                                {
                                                    Program.MainForm.LogAppend($"Almost done. Connections left: {Pinger.CurrentLoggingConnections}, timeout {(date - startTime) > timeout * 1000}");
                                                    if (sentPacket == false)
                                                    {
                                                        var _pw = new Packet(ISClientMessages.ServerMigrationUpdate);
                                                        _pw.WriteByte((byte)ServerMigrationStatus.PlayersMigrated);
                                                        SendPacket(_pw);
                                                        sentPacket = true;
                                                        Program.MainForm.LogAppend("Sent Migration Done packet");
                                                    }
                                                    else
                                                    {
                                                        Program.MainForm.Shutdown();
                                                    }
                                                }
                                                else if (!disconnectedPlayers)
                                                {
                                                    Server.Instance.PlayerList.ForEach(x =>
                                                    {
                                                        if (x.Value.Character == null) return;
                                                        try
                                                        {
                                                            x.Value.Character.CleanupInstances();
                                                            x.Value.Socket.DoChangeChannelReq(Server.Instance.ID);
                                                        }
                                                        catch { }
                                                    });
                                                    disconnectedPlayers = true;
                                                }
                                                else
                                                {
                                                    Program.MainForm.LogAppend("Waiting for DC...");
                                                }
                                            },
                                            0,
                                            5000
                                        );
                                        break;
                                    }

                                case ServerMigrationStatus.PlayersMigrated:
                                    {
                                        Server.Instance.InMigration = false;
                                        Program.MainForm.LogAppend("Other server is done");
                                        break;
                                    }

                                case ServerMigrationStatus.StartMigration:
                                    {
                                        Server.Instance.InMigration = true;
                                        Server.Instance.IsNewServerInMigration = false;
                                        Program.MainForm.LogAppend("Started migration to new server");
                                        Server.Instance.StopListening();
                                        pw.WriteByte((byte)ServerMigrationStatus.StartListening);
                                        SendPacket(pw);
                                        break;
                                    }
                            }
                            break;
                        }

                    default:
                        if (!TryHandlePartyPacket(packet, msg) &&
                            !TryHandlePlayerPacket(packet, msg))
                        {
                            Program.MainForm.LogAppend("UNKOWN CENTER PACKET: " + packet);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Program.MainForm.LogAppend(ex + "\r\nPACKET: " + packet);
            }
        }

        public bool TryHandlePlayerPacket(Packet packet, ISServerMessages msg)
        {
            switch (msg)
            {
                case ISServerMessages.PlayerChangeServerResult:
                    {
                        string session = packet.ReadString();
                        Player player = Server.Instance.GetPlayer(session);
                        if (player != null)
                        {
                            player.Socket.StartLogging();
                            int charid = packet.ReadInt();
                            byte[] ip = packet.ReadBytes(4);
                            ushort port = packet.ReadUShort();
                            if (port == 0)
                            {
                                Packet pw = new Packet(ServerMessages.TRANSFER_CHANNEL_REQ_IGNORED);
                                player.Character.SendPacket(pw);
                            }
                            else
                            {
                                player.Character.CleanupInstances();
                                RedisBackend.Instance.SetPlayerCCIsBeingProcessed(charid);

                                player.IsCC = true;
                                player.Socket.SendConnectToServer(ip, port);
                            }
                        }
                        else
                        {
                            Program.MainForm.LogAppend("Tried to CC unknown player (unknown hash)");
                        }

                        break;
                    }

                case ISServerMessages.PlayerWhisperOrFindOperationResult:
                    {
                        bool whisper = packet.ReadBool();
                        bool found = packet.ReadBool();
                        int victim = packet.ReadInt();
                        Character victimChar = Server.Instance.GetCharacter(victim);
                        if (victimChar == null) break;
                        victimChar.Player.Socket.StartLogging();

                        if (whisper)
                        {
                            if (found)
                            {
                                string sender = packet.ReadString();
                                byte channel = packet.ReadByte();
                                string message = packet.ReadString();
                                bool direction = packet.ReadBool();
                                byte directionByte = 18;
                                if (direction)
                                {
                                    directionByte = 10;
                                }
                                MessagePacket.Whisper(victimChar, sender, channel, message, directionByte);
                            }
                            else
                            {
                                string sender = packet.ReadString();
                                MessagePacket.Find(victimChar, sender, -1, 0, false);

                            }
                        }
                        else
                        {
                            if (found)
                            {
                                string sender = packet.ReadString();
                                sbyte channel = packet.ReadSByte();
                                sbyte wat = packet.ReadSByte();
                                MessagePacket.Find(victimChar, sender, channel, wat, false);
                            }
                            else
                            {
                                string sender = packet.ReadString();
                                MessagePacket.Find(victimChar, sender, -1, 0, false);
                            }


                        }
                        break;
                    }

                case ISServerMessages.PlayerSuperMegaphone:
                    {
                        MessagePacket.SendSuperMegaphoneMessage(packet.ReadString(), packet.ReadBool(), packet.ReadByte());
                        break;
                    }

                case ISServerMessages.AdminMessage:
                    {
                        string message = packet.ReadString();
                        byte type = packet.ReadByte();

                        Packet pw = new Packet(ServerMessages.BROADCAST_MSG);
                        pw.WriteByte(type);
                        pw.WriteString(message);
                        if (type == 4)
                        {
                            pw.WriteBool(message.Length != 0);
                        }

                        foreach (var kvp in DataProvider.Maps)
                        {
                            kvp.Value.SendPacket(pw);
                        }
                        break;
                    }

                case ISServerMessages.PlayerChangeServerData:
                    {
                        var charid = packet.ReadInt();
                        var readBufferPacket = new Packet(packet.ReadLeftoverBytes());
                        Server.Instance.CCIngPlayerList[charid] = new Tuple<Packet, long>(readBufferPacket, MasterThread.CurrentTime);
                        break;
                    }

                case ISServerMessages.KickPlayerResult:
                    {
                        int userId = packet.ReadInt();
                        Player player = Server.Instance.PlayerList.Values.FirstOrDefault(p => p.Character != null && p.Character.UserID == userId);
                        if (player != null)
                        {
                            Program.MainForm.LogAppend("Handling centerserver kick request for user " + userId);
                            player.Socket.Disconnect();
                        }
                        break;
                    }

                case ISServerMessages.PlayerSendPacket:
                    {
                        Character pChar = Server.Instance.GetCharacter(packet.ReadInt());
                        ////Console.WriteLine(pChar.Name);
                        pChar?.SendPacket(packet.ReadLeftoverBytes());
                        break;
                    }
                default: return false;
            }
            return true;
        }

        public bool TryHandlePartyPacket(Packet packet, ISServerMessages msg)
        {
            switch (msg)
            {

                case ISServerMessages.ChangeParty:
                    {
                        Character fucker = Server.Instance.GetCharacter(packet.ReadInt());
                        if (fucker != null)
                            fucker.PartyID = packet.ReadInt();
                        break;
                    }

                case ISServerMessages.UpdateHpParty:
                    {
                        Character fucker = Server.Instance.GetCharacter(packet.ReadInt());
                        if (fucker != null && fucker.PartyID != 0)
                        {
                            fucker.FullPartyHPUpdate();
                        }
                        break;
                    }

                case ISServerMessages.PartyInformationUpdate:
                    {
                        int ptId = packet.ReadInt();
                        int leader = packet.ReadInt();
                        var members = new int[Constants.MaxPartyMembers];
                        for (int i = 0; i < members.Length; i++)
                        {
                            members[i] = packet.ReadInt();
                        }
                        PartyData pd;
                        if (PartyData.Parties.TryGetValue(ptId, out pd))
                        {
                            pd.Members = members;
                        }
                        else
                        {
                            pd = PartyData.Parties[ptId] = new PartyData(leader, members, ptId);
                        }
                        PartyData.TryUpdatePartyDataInInstances(pd);
                        break;
                    }

                case ISServerMessages.PartyDisbanded:
                    {
                        int ptId = packet.ReadInt();
                        //Program.MainForm.LogDebug("Removing party: " + ptId);
                        PartyData.Parties.Remove(ptId);
                        break;
                    }

                default: return false;
            }

            return true;
        }

    }
}
