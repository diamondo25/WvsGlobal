using System;
using System.Net;
using MySql.Data.MySqlClient;
using WvsBeta.Common.Character;
using WvsBeta.Common.Sessions;

namespace WvsBeta.Login
{
    public class CenterSocket : AbstractConnection
    {
        private Center _center;

        public CenterSocket(string ip, ushort port, Center center)
            : base(ip, port)
        {
            _center = center;
        }

        public override void OnDisconnect()
        {
            Program.MainForm.LogAppend("Disconnected from the CenterServer!");
            // release all connections
        }

        public override void OnHandshakeInbound(Packet pPacket)
        {
            Packet packet = new Packet(ISClientMessages.ServerRequestAllocation);
            packet.WriteString(Server.Instance.Name);
            packet.WriteString(Server.Instance.PublicIP.ToString());
            packet.WriteUShort(Server.Instance.Port);
            SendPacket(packet);

            Program.MainForm.LogAppend("Connected to the CenterServer!");
        }

        public override void AC_OnPacketInbound(Packet packet)
        {
            try
            {
                switch ((ISServerMessages)packet.ReadByte())
                {
                    case ISServerMessages.ChangeCenterServer:
                        {
                            var ip = packet.ReadBytes(4);
                            var port = packet.ReadUShort();
                            _center.IP = new IPAddress(ip);
                            _center.Port = port;
                            this.Disconnect();
                            _center.Connect();
                            break;
                        }
                    case ISServerMessages.ServerSetUserNo:
                        {
                            for (var i = 0; i < _center.Channels; i++)
                            {
                                _center.UserNo[i] = packet.ReadInt();
                            }
                            break;
                        }

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

                                player.Socket.ConnectToServer(charid, ip, port);
                            }
                            break;
                        }
                    case ISServerMessages.PlayerRequestWorldLoadResult:
                        {
                            string session = packet.ReadString();
                            Player player = Server.Instance.GetPlayer(session);
                            player?.Socket.StartLogging();
                            player?.Socket.HandleWorldLoadResult(packet);

                            break;
                        }
                    case ISServerMessages.PlayerRequestChannelStatusResult:
                        {
                            string session = packet.ReadString();
                            Player player = Server.Instance.GetPlayer(session);
                            if (player == null) return;
                            player.Socket.StartLogging();

                            byte ans = packet.ReadByte();
                            if (ans != 0x00)
                            {
                                Packet pack = new Packet(ServerMessages.SELECT_WORLD_RESULT);
                                pack.WriteByte(ans);
                                player.Socket.SendPacket(pack);
                            }
                            else
                            {
                                player.Socket.HandleChannelSelectResult(packet);
                            }


                            break;
                        }
                    case ISServerMessages.PlayerCreateCharacterResult:
                        {
                            var hash = packet.ReadString();
                            Player player = Server.Instance.GetPlayer(hash);
                            if (player == null) return;
                            player.Socket.StartLogging();

                            player.Socket.HandleCreateNewCharacterResult(packet);

                            break;
                        }

                    case ISServerMessages.PlayerCreateCharacterNamecheckResult:
                        {
                            var hash = packet.ReadString();
                            Player player = Server.Instance.GetPlayer(hash);
                            if (player == null) return;
                            player.Socket.StartLogging();

                            var name = packet.ReadString();
                            var taken = packet.ReadBool();

                            var pack = new Packet(ServerMessages.CHECK_CHARACTER_NAME_AVAILABLE);
                            pack.WriteString(name);
                            pack.WriteBool(taken);
                            player.Socket.SendPacket(pack);

                            break;
                        }

                    case ISServerMessages.PlayerDeleteCharacterResult:
                        {
                            var hash = packet.ReadString();
                            Player player = Server.Instance.GetPlayer(hash);
                            if (player == null) return;
                            player.Socket.StartLogging();

                            int charid = packet.ReadInt();
                            byte result = packet.ReadByte();
                            player.Socket.HandleCharacterDeletionResult(charid, result);

                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                Program.MainForm.LogAppend("Exception while handling packet from CenterServer: {0}", ex);
            }
        }

        public void UpdateConnections(int value)
        {
            Packet packet = new Packet(ISClientMessages.ServerSetConnectionsValue);
            packet.WriteInt(value);
            SendPacket(packet);
        }

        public void RequestCharacterConnectToWorld(string Hash, int charid, byte world, byte channel)
        {
            Packet packet = new Packet(ISClientMessages.PlayerChangeServer);
            packet.WriteString(Hash);
            packet.WriteInt(charid);
            packet.WriteByte(world);
            packet.WriteByte(channel);
            packet.WriteBool(false);
            SendPacket(packet);
        }

        public void RequestCharacterGetWorldLoad(string Hash, byte world)
        {
            Packet packet = new Packet(ISClientMessages.PlayerRequestWorldLoad);
            packet.WriteString(Hash);
            packet.WriteByte(world);
            SendPacket(packet);
        }

        public void CheckCharacternameTaken(string Hash, string name)
        {
            Packet packet = new Packet(ISClientMessages.PlayerCreateCharacterNamecheck);
            packet.WriteString(Hash);
            packet.WriteString(name);
            SendPacket(packet);
        }

        public void RequestCharacterIsChannelOnline(string Hash, byte world, byte channel, int accountId)
        {
            Packet packet = new Packet(ISClientMessages.PlayerRequestChannelStatus);
            packet.WriteString(Hash);
            packet.WriteByte(world);
            packet.WriteByte(channel);
            packet.WriteInt(accountId);
            SendPacket(packet);
        }

        public void RequestDeleteCharacter(string Hash, int accountId, int characterId)
        {
            Packet packet = new Packet(ISClientMessages.PlayerDeleteCharacter);
            packet.WriteString(Hash);
            packet.WriteInt(accountId);
            packet.WriteInt(characterId);
            SendPacket(packet);
        }

    }
}
