using System;
using System.Net;
using System.Windows.Forms;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;

namespace WvsBeta.Shop
{
    public class CenterSocket : AbstractConnection
    {
        private bool disconnectExpected = false;
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
                Application.Exit();
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
            switch ((ISServerMessages)packet.ReadByte())
            {
                case ISServerMessages.ChangeCenterServer:
                    {
                        var ip = packet.ReadBytes(4);
                        var port = packet.ReadUShort();
                        disconnectExpected = true;
                        Server.Instance.CenterIP = new IPAddress(ip);
                        Server.Instance.CenterPort = port;
                        Server.Instance.CenterMigration = true;
                        this.Disconnect();
                        break;
                    }

                case ISServerMessages.PlayerChangeServerResult:
                    {
                        string session = packet.ReadString();
                        Player player = Server.Instance.GetPlayer(session);
                        if (player != null)
                        {
                            int charid = packet.ReadInt();
                            byte[] ip = packet.ReadBytes(4);
                            ushort port = packet.ReadUShort();
                            if (port == 0)
                            {
                                player.Socket.Disconnect();
                            }
                            else
                            {
                                RedisBackend.Instance.SetPlayerCCIsBeingProcessed(charid);

                                player.IsCC = true;
                                player.Socket.SendConnectToServer(ip, port);
                            }
                        }

                        break;
                    }
                case ISServerMessages.ServerAssignmentResult:
                    {
                        if (!Server.Instance.CenterMigration)
                        {
                            Server.Instance.StartListening();

                            Program.MainForm.LogAppend($"Handling as CashShop on World {Server.Instance.WorldID} ({Server.Instance.WorldName})");
                        }
                        else
                        {
                            Program.MainForm.LogAppend("Reconnected to center server");
                        }

                        Server.Instance.CenterMigration = false;
                        break;
                    }

                case ISServerMessages.PlayerChangeServerData:
                    {
                        var charid = packet.ReadInt();
                        var readBufferPacket = new Packet(packet.ReadLeftoverBytes());
                        Server.Instance.CCIngPlayerList[charid] = readBufferPacket;
                        break;
                    }
                case ISServerMessages.ReloadCashshopData:
                    {
                        Program.MainForm.LogAppend("Reloading cashshop data");
                        Server.Instance.LoadCashshopData();
                        DataProvider.Reload();
                        break;
                    }

                default: break;
            }
        }

        public void updateConnections(int value)
        {
            Packet packet = new Packet(ISClientMessages.ServerSetConnectionsValue);
            packet.WriteInt(value);
            SendPacket(packet);
        }

        public void CharacterExitCashshop(string Hash, int charid, byte world)
        {
            Packet packet = new Packet(ISClientMessages.PlayerQuitCashShop);
            packet.WriteString(Hash);
            packet.WriteInt(charid);
            packet.WriteByte(world);
            if (Server.Instance.CCIngPlayerList.TryGetValue(charid, out Packet p))
            {
                Server.Instance.CCIngPlayerList.Remove(charid);
                packet.WriteBytes(p.ReadLeftoverBytes());
            }
            else
            {
                packet.WriteInt(0);
            }

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

    }
}