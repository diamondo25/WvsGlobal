using System;
using System.Threading;
using log4net;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;

namespace WvsBeta.Shop
{
    public class ClientSocket : AbstractConnection
    {
        public Player Player { get; set; }
        public bool Loaded { get; set; }

        public ClientSocket(System.Net.Sockets.Socket pSocket)
            : base(pSocket)
        {
            Loaded = false;
            Init();
        }

        private void Init()
        {
            Player = new Player()
            {
                Socket = this,
                Character = null
            };
            Server.Instance.AddPlayer(Player);

            SendHandshake(Constants.MAPLE_VERSION, Constants.MAPLE_PATCH_LOCATION, Constants.MAPLE_LOCALE);
            SendMemoryRegions();

            Pinger.Add(this);
        }


        public override void StartLogging()
        {
            base.StartLogging();
            Player?.Character?.SetupLogging();
        }

        public override void EndLogging()
        {
            base.EndLogging();
            Character.RemoveLogging();
        }

        public override void OnDisconnect()
        {
            try
            {
                StartLogging();
                if (Player != null)
                {
                    if (Loaded && Player.Character != null)
                    {
                        Program.MainForm.LogAppend($"{Player.Character.Name} disconnected!");

                        var chr = Player.Character;
                        var cc = Player.IsCC;

                        Server.Instance.CharacterList.Remove(chr.ID);

                        chr.Save();

                        Server.Instance.CenterConnection.UnregisterCharacter(chr.ID, cc);

                        Program.MainForm.ChangeLoad(false);

                        if (!cc)
                        {
                            RedisBackend.Instance.RemovePlayerOnline(chr.UserID);
                        }

                        RedisBackend.Instance.RemovePlayerCCIsBeingProcessed(chr.ID);

                        Player.Character = null;
                    }
                    Player.Socket = null;
                    Server.Instance.RemovePlayer(Player.SessionHash);
                }
            }
            catch (Exception ex)
            {
                ////Console.WriteLine(ex.ToString());
            }
            finally
            {
                EndLogging();
            }
            Pinger.Remove(this);
        }

        public override void AC_OnPacketInbound(Packet packet)
        {
            try
            {
                StartLogging();
                var header = (ClientMessages)packet.ReadByte();
                if (!Loaded)
                {
                    switch (header)
                    {
                        case ClientMessages.MIGRATE_IN:
                            OnPlayerLoad(packet);
                            break;
                    }
                }
                else
                {
                    switch (header)
                    {
                        case ClientMessages.ENTER_PORTAL:
                            Server.Instance.CenterConnection.CharacterExitCashshop(Player.SessionHash,
                                Player.Character.ID, Server.Instance.WorldID);
                            break;
                        case ClientMessages.CASHSHOP_ACTION:
                            CashPacket.HandleCashPacket(Player.Character, packet);
                            break;
                        case ClientMessages.CASHSHOP_QUERY_CASH:
                            CashPacket.SendCashAmounts(Player.Character);
                            break;
                        case ClientMessages.CLIENT_HASH: break;
                        case ClientMessages.PONG:
                            // Make sure we update the player online thing
                            RedisBackend.Instance.SetPlayerOnline(
                                Player.Character.UserID,
                                Server.Instance.GetOnlineId()
                            );
                            break;
                        default:
                            {
                                Program.MainForm.LogAppend("[" + header + "] Unknown packet: " + packet);
                                break;
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                Program.MainForm.LogAppend("Exception: " + ex);
            }
            finally
            {
                EndLogging();
            }
        }

        public override void OnHackDetected()
        {
            TryRegisterHackDetection();
        }

        public void TryRegisterHackDetection()
        {
            if (!Loaded) return;
            TryRegisterHackDetection(Player.Character.UserID);
        }


        public void SendConnectToServer(byte[] IP, ushort port, bool noScheduledDisconnect = false)
        {
            Packet pw = new Packet(ServerMessages.CHANGE_CHANNEL);
            pw.WriteBool(true);
            pw.WriteBytes(IP);
            pw.WriteUShort(port);
            SendPacket(pw);

            if (!noScheduledDisconnect)
            {
                ScheduleDisconnect();
            }
        }

        public void OnPlayerLoad(Packet packet)
        {
            int characterId = packet.ReadInt();

            ThreadContext.Properties["CharacterID"] = characterId;

            if (RedisBackend.Instance.HoldoffPlayerConnection(characterId))
            {
                Program.MainForm.LogAppend("Bouncing charid: " + characterId);
                SendConnectToServer(Server.Instance.PublicIP.GetAddressBytes(), Server.Instance.Port, true);
                return;
            }


            if (RedisBackend.Instance.PlayerIsMigrating(characterId, true) == false)
            {
                Program.MainForm.LogAppend("Disconnecting because not migrating. Charid: " + characterId);
                goto cleanup_and_disconnect;
            }


            if (Server.Instance.CharacterList.ContainsKey(characterId))
            {
                Program.MainForm.LogAppend($"Player tried to login while already being loggedin. Playerid: {characterId}");
                goto cleanup_and_disconnect;
            }

            var character = new Character(characterId);
            var loadResult = character.Load(IP);
            if (loadResult != Character.LoadFailReasons.None)
            {
                Program.MainForm.LogAppend($"Player tried to login, but we failed loading the char! Playerid: {characterId}, reason {loadResult}");
                goto cleanup_and_disconnect;
            }

            Player.Character = character;
            character.Player = Player;

            Program.MainForm.LogAppend($"{character.Name} connected!");
            Program.MainForm.ChangeLoad(true);
            Server.Instance.CharacterList.Add(characterId, character);

            Server.Instance.CenterConnection.RegisterCharacter(
                characterId, 
                character.Name,
                character.CharacterStat.Job,
                character.CharacterStat.Level, 
                character.GMLevel
            );

            Loaded = true;

            MapPacket.SendJoinCashServer(character);
            CashPacket.SendInfo(character);

            TryRegisterHackDetection();

            return;
            cleanup_and_disconnect:

            Server.Instance.CCIngPlayerList.Remove(characterId);
            Disconnect();
        }
    }
}