using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using log4net;
using MySql.Data.MySqlClient;
using WvsBeta.Common;
using WvsBeta.Common.Character;
using WvsBeta.Common.Sessions;
using System.Text;
using log4net.Appender;

namespace WvsBeta.Login
{
    public class ClientSocket : AbstractConnection
    {
        private static ILog log = LogManager.GetLogger("LoginLogic");

        public Player Player { get; private set; }
        public bool Loaded { get; set; }
        private string machineID;
        private byte? autoSelectChar = null;
        public bool IsCCing { get; private set; }

        public ClientSocket(System.Net.Sockets.Socket pSocket)
            : base(pSocket)
        {
            Player = new Player()
            {
                LoggedOn = false,
                Socket = this
            };
            Loaded = false;
            Pinger.Add(this);
            Server.Instance.AddPlayer(Player);
            machineID = "";

            SendHandshake(Constants.MAPLE_VERSION, Constants.MAPLE_PATCH_LOCATION, Constants.MAPLE_LOCALE);
            SendMemoryRegions();
        }

        public override void StartLogging()
        {
            base.StartLogging();

            log4net.ThreadContext.Properties["LoginState"] = Player.State;
            if (Loaded)
            {
                log4net.ThreadContext.Properties["UserID"] = Player.ID;
            }
        }

        public override void EndLogging()
        {
            base.EndLogging();
            log4net.ThreadContext.Properties.Remove("UserID");
            log4net.ThreadContext.Properties.Remove("LoginState");
        }

        public override void OnDisconnect()
        {
            try
            {
                StartLogging();
                try
                {
                    if (crashLogTmp != null)
                    {
                        FileWriter.WriteLine(Path.Combine("ClientCrashes", base.IP + "-unknown_username.txt"),
                            crashLogTmp);
                        crashLogTmp = null;
                    }
                }
                catch { }

                if (Player != null)
                {
                    Server.Instance.RemovePlayer(Player.SessionHash);
                    if (Player.LoggedOn)
                    {
                        Program.MainForm.ChangeLoad(false);

                        Player.Characters.Clear();

                        if (!IsCCing)
                            RedisBackend.Instance.RemovePlayerOnline(Player.ID);

                        Player.Socket = null;
                        Player = null;
                    }

                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
            finally
            {
                EndLogging();
            }
            Pinger.Remove(this);
        }

        private string crashLogTmp = null;

        private static HashSet<ClientMessages> logIgnore = new HashSet<ClientMessages>() { ClientMessages.CLIENT_HASH, ClientMessages.PONG, ClientMessages.LOGIN_CHECK_PASSWORD, ClientMessages.LOGIN_CHECK_PIN, ClientMessages.LOGIN_WORLD_INFO_REQUEST, ClientMessages.LOGIN_SELECT_CHANNEL };

        public override void AC_OnPacketInbound(Packet packet)
        {
            try
            {
                ClientMessages header = (ClientMessages)packet.ReadByte();


                if (!logIgnore.Contains(header))
                    Common.Tracking.PacketLog.ReceivedPacket(packet, (byte)header, Server.Instance.Name, this.IP);

                if (!Loaded)
                {
                    switch (header)
                    {
                        case ClientMessages.LOGIN_CHECK_PASSWORD:
                            OnCheckPassword(packet);
                            break;
                        case ClientMessages.CLIENT_CRASH_REPORT:
                            crashLogTmp = packet.ReadString();
                            if (crashLogTmp.Contains("LdrShutdownProcess"))
                            {
                                // Ignore
                                crashLogTmp = null;
                            }
                            else
                            {
                                Program.MainForm.LogAppend("Received a crashlog!!!");
                            }
                            break;
                        case ClientMessages.LOGIN_EULA:
                            OnConfirmEULA(packet);
                            break;
                    }
                }
                else
                {
                    switch (header)
                    {
                        // Ignore this one
                        case ClientMessages.LOGIN_CHECK_PASSWORD: break;

                        case ClientMessages.LOGIN_SELECT_CHANNEL:
                            OnChannelSelect(packet);
                            break;
                        case ClientMessages.LOGIN_WORLD_INFO_REQUEST:
                            OnWorldInfoRequest(packet);
                            break;
                        case ClientMessages.LOGIN_WORLD_SELECT:
                            OnWorldSelect(packet);
                            break;
                        case ClientMessages.LOGIN_CHECK_CHARACTER_NAME:
                            OnCharNamecheck(packet);
                            break;
                        case ClientMessages.LOGIN_SELECT_CHARACTER:
                            OnSelectCharacter(packet);
                            break;
                        case ClientMessages.LOGIN_SET_GENDER:
                            OnSetGender(packet);
                            break;
                        case ClientMessages.LOGIN_CHECK_PIN:
                            OnPinCheck(packet);
                            break;
                        case ClientMessages.LOGIN_CREATE_CHARACTER:
                            OnCharCreation(packet);
                            break;
                        case ClientMessages.LOGIN_DELETE_CHARACTER:
                            OnCharDeletion(packet);
                            break;
                        case ClientMessages.PONG:
                            RedisBackend.Instance.SetPlayerOnline(Player.ID, 1);
                            break;
                        case ClientMessages.CLIENT_HASH: break;
                        default:
                            {
                                var errorText = "Unknown packet found " + packet;
                                Server.Instance.ServerTraceDiscordReporter.Enqueue(errorText);
                                Program.MainForm.LogAppend(errorText);

                                break;
                            }
                    }
                }
             }
            catch (Exception ex)
            {
                var errorText = "Exception caught: " + ex + ", packet: " + packet;
                Server.Instance.ServerTraceDiscordReporter.Enqueue(errorText);
                Program.MainForm.LogAppend(errorText);
                log.Error(ex);
                Disconnect();
            }
        }

        public override void OnHackDetected()
        {
            TryRegisterHackDetection();
        }

        public void TryRegisterHackDetection()
        {
            if (!Loaded) return;
            TryRegisterHackDetection(Player.ID);
        }
        

        private bool AssertWarning(bool assertion, string msg)
        {
            if (assertion)
            {
                log.Warn(msg);
                Server.Instance.ServerTraceDiscordReporter.Enqueue($"AssertWarning: {msg}");
            }
            return assertion;
        }

        private bool AssertError(bool assertion, string msg)
        {
            if (assertion)
            {
                log.Error(msg);
                Program.MainForm.LogAppend(msg);
                Server.Instance.ServerTraceDiscordReporter.Enqueue($"AssertError: {msg}");
            }
            return assertion;
        }

        public bool IsValidName(string pName)
        {
            if (AssertWarning(Player.Characters.Count >= 3, "Reached maximum amount of characters and still did a namecheck.")) return false;
            if (AssertWarning(pName.Length < 4 || pName.Length > 12, "Name length invalid!")) return false;
            if (AssertWarning(pName.Any(x =>
            {
                if (x >= 'a' && x <= 'z') return false;
                if (x >= 'A' && x <= 'Z') return false;
                if (x >= '0' && x <= '9') return false;
                return true;
            }), "Name had invalid characters: " + pName)) return false;

            if (AssertWarning(WzReader.ForbiddenName.Exists(pName.Contains),
                "Charactername matched a ForbiddenName item. " + pName)) return false;

            return true;
        }

        public void ConnectToServer(int charid, byte[] IP, ushort port)
        {
            byte bit = 0, goPremium = 0;

            bit |= (byte)(goPremium << 1);

            log.Info($"Connecting to {IP[0]}.{IP[1]}.{IP[2]}.{IP[3]}:{port} world {Player.World} channel {Player.Channel} with charid {charid} name {Player.Characters[charid]}");

            IsCCing = true;

            Packet pw = new Packet(ServerMessages.SELECT_CHARACTER_RESULT);
            pw.WriteByte(0);
            pw.WriteByte(0);
            pw.WriteBytes(IP);
            pw.WriteUShort(port);
            pw.WriteInt(charid);
            pw.WriteByte(bit);
            SendPacket(pw);
        }

        public void OnSelectCharacter(Packet packet)
        {
            if (AssertWarning(
                Player.State != Player.LoginState.CharacterSelect &&
                Player.State != Player.LoginState.CharacterCreation, "Trying to select character while not in character select screen.")) return;
            int charid = packet.ReadInt();

            if (AssertWarning(Player.HasCharacterWithID(charid) == false, "Trying to select a character that the player doesnt have. ID: " + charid)) return;
            
            if (Server.Instance.GetWorld(Player.World, out Center center))
            {
                center.Connection.RequestCharacterConnectToWorld(Player.SessionHash, charid, Player.World, Player.Channel);
                return;
            }

            // Server is offline
            var pw = new Packet(ServerMessages.SELECT_CHARACTER_RESULT);
            pw.WriteByte(6); // Connection failed due to system error
            pw.WriteByte(0);
            SendPacket(pw);
        }

        public void OnCharNamecheck(Packet packet)
        {
            if (AssertWarning(Player.State != Player.LoginState.CharacterSelect && Player.State != Player.LoginState.CharacterCreation, "Trying to check character name while not in character select or creation screen.")) return;

            Player.State = Player.LoginState.CharacterCreation;

            string name = packet.ReadString();

            if (!IsValidName(name))
            {
                Packet pack = new Packet(ServerMessages.CHECK_CHARACTER_NAME_AVAILABLE);
                pack.WriteString(name);
                pack.WriteBool(true);
                SendPacket(pack);
                return;
            }

            if (Server.Instance.GetWorld(Player.World, out Center center))
            {
                center.Connection.CheckCharacternameTaken(Player.SessionHash, name);
            }
            else
            {
                AssertWarning(true, "Server was offline while checking for duplicate charname");
                Packet pack = new Packet(ServerMessages.CHECK_CHARACTER_NAME_AVAILABLE);
                pack.WriteString(name);
                pack.WriteBool(true);
                SendPacket(pack);
            }

        }

        public void OnCharDeletion(Packet packet)
        {
            if (AssertWarning(
                Player.State != Player.LoginState.CharacterSelect &&
                Player.State != Player.LoginState.CharacterCreation,
                "Trying to delete character while not in character select or create screen.")) return;

            int DoB = packet.ReadInt();
            int charid = packet.ReadInt();

            if (AssertWarning(Player.HasCharacterWithID(charid) == false, "Trying to delete a character that the player doesnt have. ID: " + charid)) return;

            if (Player.DateOfBirth != DoB)
            {
                log.Warn("Invalid DoB entered when trying to delete character.");

                var pack = new Packet(ServerMessages.DELETE_CHARACTER_RESULT);
                pack.WriteInt(charid);
                pack.WriteByte(18);
                SendPacket(pack);
                return;
            }

            if (!Server.Instance.GetWorld(Player.World, out Center center))
            {
                log.Error("Unable to connect to center server?");
                var pack = new Packet(ServerMessages.DELETE_CHARACTER_RESULT);
                pack.WriteInt(charid);
                pack.WriteByte(10);
                SendPacket(pack);
                return;
            }


            center.Connection?.RequestDeleteCharacter(Player.SessionHash, Player.ID, charid);
        }

        public void HandleCharacterDeletionResult(int characterId, byte result)
        {
            if (result == 0)
            {
                log.Info($"User deleted a character, called '{Player.Characters[characterId]}'");
                // Alright!
                Player.Characters.Remove(characterId);
            }

            var pack = new Packet(ServerMessages.DELETE_CHARACTER_RESULT);
            pack.WriteInt(characterId);
            pack.WriteByte(result);
            SendPacket(pack);
        }

        private bool IsValidCreationId(IEnumerable<int> validIds, int inputId, string name)
        {
            if (validIds.Contains(inputId)) return true;
            AssertError(true, $"[CharCreation] Invalid {name}: {inputId}");
            return false;
        }

        public void OnCharCreation(Packet packet)
        {
            if (AssertWarning(Player.State != Player.LoginState.CharacterCreation, "Trying to create character while not in character creation screen (skipped namecheck?).")) return;

            if (!Server.Instance.GetWorld(Player.World, out Center center))
            {
                log.Error("Unable to connect to center server?");
                goto not_available;
            }

            if (center.BlockCharacterCreation)
            {
                log.Error("Character creation blocked!");
                goto not_available;
            }

            Packet pack;
            string charname = packet.ReadString();

            if (!IsValidName(charname))
            {
                goto not_available;
            }

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

            AssertWarning(str >= 13 || dex >= 13 || intt >= 13 || luk >= 13, $" '{charname}'  is under suspicion of using Cheat Engine to get 13 stat ({str}/{dex}/{intt}/{luk}) during character creation.");

            if (!(str >= 4 && dex >= 4 && intt >= 4 && luk >= 4 && (str + dex + intt + luk) <= 25))
            {
                log.Error($"Invalid stats for character creation: {str} {dex} {intt} {luk}");
                goto not_available;
            }

            var cci = Player.Gender == 0 ? CreateCharacterInfo.Male : CreateCharacterInfo.Female;

            if (!IsValidCreationId(cci.Face, face, "face") ||
                !IsValidCreationId(cci.Hair, hair, "hair") ||
                !IsValidCreationId(cci.HairColor, haircolor, "haircolor") ||
                !IsValidCreationId(cci.Skin, skin, "skin") ||
                !IsValidCreationId(cci.Coat, top, "top") ||
                !IsValidCreationId(cci.Pants, bottom, "bottom") ||
                !IsValidCreationId(cci.Shoes, shoes, "shoes") ||
                !IsValidCreationId(cci.Weapon, weapon, "weapon"))
            {
                AssertError(true, $"User tried to create account with wrong starter equips. {face} {hair} {haircolor} {skin} {top} {bottom} {shoes} {weapon}");
                goto not_available;
            }


            pack = new Packet(ISClientMessages.PlayerCreateCharacter);
            pack.WriteString(Player.SessionHash);
            pack.WriteInt(Player.ID);
            pack.WriteByte(Player.Gender);

            pack.WriteString(charname);

            pack.WriteInt(face);
            pack.WriteInt(hair);
            pack.WriteInt(haircolor);
            pack.WriteInt(skin);

            pack.WriteInt(top);
            pack.WriteInt(bottom);
            pack.WriteInt(shoes);
            pack.WriteInt(weapon);

            pack.WriteByte(str);
            pack.WriteByte(dex);
            pack.WriteByte(intt);
            pack.WriteByte(luk);

            center.Connection.SendPacket(pack);

            return;

            not_available:
            pack = new Packet(ServerMessages.CREATE_NEW_CHARACTER_RESULT);
            pack.WriteByte(1);
            SendPacket(pack);

        }

        public void HandleCreateNewCharacterResult(Packet packet)
        {
            var pack = new Packet(ServerMessages.CREATE_NEW_CHARACTER_RESULT);
            if (packet.ReadBool())
            {
                // Succeeded
                pack.WriteBool(false);
                var ad = new AvatarData();
                ad.Decode(packet);
                ad.Encode(pack);


                log.Info($"User created a new character, called '{ad.CharacterStat.Name}'");
                Player.Characters.Add(ad.CharacterStat.ID, ad.CharacterStat.Name);
                Player.State = Player.LoginState.CharacterSelect;
            }
            else
            {
                pack.WriteBool(true);
            }
            Player.Socket.SendPacket(pack);
        }

        public void OnChannelSelect(Packet packet)
        {
            if (AssertWarning(Player.State != Player.LoginState.ChannelSelect,
                "Tried to select channel while not in channel select.")) return;

            var worldId = packet.ReadByte();
            var channelId = packet.ReadByte();


            if (worldId != Player.World ||
                !Server.Instance.GetWorld(Player.World, out Center center) ||
                channelId >= center.Channels)
            {
                var p = new Packet(ServerMessages.SELECT_WORLD_RESULT);
                p.WriteByte(8);
                SendPacket(p);
                return;
            }

            center.Connection?.RequestCharacterIsChannelOnline(Player.SessionHash, Player.World, channelId, Player.ID);
        }

        public void HandleChannelSelectResult(Packet packet)
        {
            // Packet received from the center server

            Player.Channel = packet.ReadByte();

            var characters = packet.ReadByte();


            var pack = new Packet(ServerMessages.SELECT_WORLD_RESULT);
            pack.WriteByte(0); //Success, other values generate error messages
            pack.WriteByte(characters);

            for (int index = 0; index < characters; index++)
            {
                var ad = new AvatarData();
                ad.Decode(packet);
                ad.Encode(pack);

                var hasRanking = packet.ReadBool();
                pack.WriteBool(hasRanking);
                if (hasRanking)
                {
                    pack.WriteInt(packet.ReadInt());
                    pack.WriteInt(packet.ReadInt());
                    pack.WriteInt(packet.ReadInt());
                    pack.WriteInt(packet.ReadInt());
                }

                Player.Characters[ad.CharacterStat.ID] = ad.CharacterStat.Name;
            }

            SendPacket(pack);

            Player.State = Player.LoginState.CharacterSelect;

            if (autoSelectChar.HasValue &&
                autoSelectChar.Value < Player.Characters.Count &&
                Server.Instance.GetWorld(Player.World, out Center center))
            {
                var charid = Player.Characters.ElementAt(autoSelectChar.Value).Key;
                center.Connection.RequestCharacterConnectToWorld(
                    Player.SessionHash,
                    charid,
                    Player.World,
                    Player.Channel
                );
            }
        }


        public void OnWorldInfoRequest(Packet packet)
        {
            if (AssertWarning(Player.State != Player.LoginState.WorldSelect,
                "Tried to get the world information while not in worldselect")) return;

            foreach (var kvp in Server.Instance.Worlds)
            {
                var world = kvp.Value;

                Packet worldInfo = new Packet(ServerMessages.WORLD_INFORMATION);
                worldInfo.WriteByte(world.ID);
                worldInfo.WriteString(world.Name);
                worldInfo.WriteByte(world.Channels); //last channel

                for (byte i = 0; i < world.Channels; i++)
                {
                    worldInfo.WriteString(world.Name + "-" + (i + 1));
                    worldInfo.WriteInt(world.UserNo[i] * 10);
                    worldInfo.WriteByte(world.ID);
                    worldInfo.WriteByte(i);
                    worldInfo.WriteBool(world.BlockCharacterCreation);
                }

                SendPacket(worldInfo);
            }

            Packet endWorldInfo = new Packet(ServerMessages.WORLD_INFORMATION);
            endWorldInfo.WriteSByte(-1);

            SendPacket(endWorldInfo);
        }

        public void OnWorldSelect(Packet packet)
        {
            if (AssertWarning(Player.State != Player.LoginState.WorldSelect && Player.State != Player.LoginState.ChannelSelect,
                "Player tried to select world while not in worldselect or channelselect")) return;

            byte worldId = packet.ReadByte();

            if (!Server.Instance.GetWorld(worldId, out Center center))
            {
                var p = new Packet(ServerMessages.CHECK_USER_LIMIT_RESULT);
                p.WriteByte(2); // Full server warning
                SendPacket(p);
                return;
            }

            Player.World = worldId;

            center.Connection.RequestCharacterGetWorldLoad(Player.SessionHash, worldId);
        }

        public void HandleWorldLoadResult(Packet packet)
        {
            Packet pack = new Packet(ServerMessages.CHECK_USER_LIMIT_RESULT);
            pack.WriteByte(packet.ReadByte());
            SendPacket(pack);

            Player.State = Player.LoginState.ChannelSelect;
        }

        public struct LoginLoggingStruct
        {
            public string localUserId { get; set; }
            public string uniqueId { get; set; }
            public string osVersion { get; set; }
            public bool adminClient { get; set; }
            public bool possibleUniqueIdBypass { get; set; }
            public string username { get; set; }
        }

        public void OnCheckPassword(Packet packet)
        {
            if (AssertWarning(Player.State != Player.LoginState.LoginScreen, "Player tried to login while not in loginscreen."))
            {
                Program.MainForm.LogAppend("Disconnected client (4)");
                Disconnect();
                return;
            }

            string username = packet.ReadString();
            string password = packet.ReadString();

            if (AssertWarning(username.Length < 4 || username.Length > 12, "Username length wrong (len: " + username.Length + "): " + username) ||
                AssertWarning(password.Length < 4 || password.Length > 12, "Password length wrong (len: " + password.Length + ")"))
            {
                Disconnect();
                return;
            }

            var lastBit = username.Substring(username.Length - 2);
            if (lastBit[0] == ':' && byte.TryParse("" + lastBit[1], out byte b))
            {
                autoSelectChar = b;
                username = username.Remove(username.Length - 2);
            }

            string machineID = string.Join("", packet.ReadBytes(16).Select(x => x.ToString("X2")));
            this.machineID = machineID;
            int startupThingy = packet.ReadInt();

            int localUserIdLength = packet.ReadShort();
            string localUserId = string.Join("", packet.ReadBytes(localUserIdLength).Select(x => x.ToString("X2")));


            bool possibleHack = packet.ReadBool();


            int uniqueIDLength = packet.ReadShort();
            string uniqueID = string.Join("", packet.ReadBytes(uniqueIDLength).Select(x => x.ToString("X2")));

            bool adminClient = packet.ReadBool();

            int magicWord = 0;

            if (adminClient)
            {
                magicWord = packet.ReadInt();
            }

            string osVersionString = packet.ReadString();

            short patchVersion = packet.ReadShort();


            void writeLoginInfo()
            {
                log.Info(new LoginLoggingStruct
                {
                    adminClient = adminClient,
                    localUserId = localUserId,
                    osVersion = osVersionString,
                    possibleUniqueIdBypass = possibleHack,
                    uniqueId = uniqueID,
                    username = username
                });
            }


            if (Server.Instance.CurrentPatchVersion > patchVersion)
            {
                writeLoginInfo();

                // Figure out how to patch
                if (Server.Instance.PatchNextVersion.TryGetValue(patchVersion, out var toVersion))
                {
                    var p = new Packet(0xC2);
                    p.WriteShort(toVersion);
                    SendPacket(p);
                    log.Info($"Sent patchexception packet ({patchVersion} -> {toVersion})");
                    return;
                }
                else
                {
                    AssertError(true, $"No patch strategy to go from {patchVersion} to {Server.Instance.CurrentPatchVersion}");
                    Disconnect();
                    return;
                }
            }

            // Okay, packet parsed
            if (adminClient)
            {
                if (AssertError(magicWord != 0x1BADD00D, $"Account '{username}' tried to login with an admin client! Magic word: {magicWord:X8}, IP: {IP}. Disconnecting."))
                {
                    writeLoginInfo();
                    Disconnect();
                    return;
                }
            }



            byte result = 9;

            string dbpass = String.Empty;
            bool updateDBPass = false;
            byte banReason = 0;
            long banExpire = 0;
            int userId = 0;

            using (var data = Server.Instance.UsersDatabase.RunQuery(
                "SELECT * FROM users WHERE username = @username",
                "@username", username
            ) as MySqlDataReader)
            {
                if (!data.Read())
                {
                    log.Warn($"[{username}] account does not exist");
                    result = 5;
                }
                else
                {
                    username = data.GetString("username");
                    userId = data.GetInt32("ID");
                    dbpass = data.GetString("password");
                    banReason = data.GetByte("ban_reason");
                    banExpire = data.GetMySqlDateTime("ban_expire").Value.ToFileTimeUtc();

                    // To fix the debug thing
                    if (false) { }
#if DEBUG
                    // Bypass for local testing
                    else if (IP == "127.0.0.1" && password == "imup2nogood")
                    {
                        result = 1;
                    }
#endif
                    else if (RedisBackend.Instance.IsPlayerOnline(userId))
                    {
                        AssertWarning(true, $"[{username}][{userId}] already online");
                        result = 7;
                    }
                    else if (banExpire > MasterThread.CurrentDate.ToUniversalTime().ToFileTimeUtc())
                    {
                        AssertWarning(true, $"[{username}][{userId}] banned until " + data.GetDateTime("ban_expire"));
                        result = 2;
                    }
                    else if (dbpass.Length > 1 && dbpass[0] != '$')
                    {
                        // Unencrypted
                        if (dbpass == password)
                        {
                            result = 1;
                            dbpass = BCrypt.HashPassword(password, BCrypt.GenerateSalt());
                            updateDBPass = true;
                        }
                        else
                        {
                            result = 4;
                        }
                    }
                    else
                    {
                        if (BCrypt.CheckPassword(password, dbpass))
                        {
                            result = 1;
                        }
                        else
                            result = 4;
                    }

                    if (result <= 1)
                    {
                        Player.ID = userId;
                        if (Server.Instance.RequiresEULA && data.GetBoolean("confirmed_eula") == false)
                        {
                            result = 19;
                        }
                        else
                        {
                            Player.GMLevel = data.GetByte("admin");
                            Player.Gender = data.GetByte("gender");
                            Player.DateOfBirth = data.GetInt32("char_delete_password");

                            Player.Username = username;
                        }
                    }
                    else if (result == 4)
                    {
                        log.Warn($"[{username}][{userId}] invalid password");
                    }
                }
            }

            bool isLoginOK = result <= 1;
            int machineBanCount = 0, uniqueBanCount = 0, ipBanCount = 0;

            if (isLoginOK)
            {
                Loaded = true;
                StartLogging();

                writeLoginInfo();

                bool macBanned = false;
                using (var mdr = Server.Instance.UsersDatabase.RunQuery(
                    "SELECT 1 FROM machine_ban WHERE machineid = @machineId OR machineid = @uniqueId",
                    "@machineId", machineID,
                    "@uniqueId", uniqueID) as MySqlDataReader)
                {
                    if (mdr.HasRows)
                    {
                        macBanned = true;
                    }
                }

                // Outside of using statement because of secondary query
                if (AssertWarning(macBanned,
                    $"[{username}][{userId}] tried to login on a machine-banned account for machineid {machineID}."))
                {
                    Disconnect();

                    Server.Instance.UsersDatabase.RunQuery(
                        "UPDATE machine_ban SET last_try = CURRENT_TIMESTAMP, last_username = @username, last_unique_id = @uniqueId, last_ip = @ip WHERE machineid = @machineId OR machineid = @uniqueId",
                        "@ip", IP,
                        "@username", username,
                        "@machineId", machineID,
                        "@uniqueId", uniqueID
                    );
                    return;
                }

                using (var mdr =
                    Server.Instance.UsersDatabase.RunQuery("SELECT 1 FROM ipbans WHERE ip = @ip", "@ip", this.IP) as
                        MySqlDataReader)
                {
                    if (mdr.HasRows)
                    {
                        AssertError(true, $"[{username}][{userId}] tried to login on a ip-banned account for ip {IP}.");
                        Disconnect();
                        return;
                    }
                }

                var (maxMachineBanCount, maxUniqueBanCount, maxIpBanCount) =
                    Server.Instance.UsersDatabase.GetUserBanRecordLimit(Player.ID);
                (machineBanCount, uniqueBanCount, ipBanCount) =
                    Server.Instance.UsersDatabase.GetBanRecord(machineID, uniqueID, IP);

                // Do not use MachineID banning, as its not unique enough
                if (ipBanCount >= maxIpBanCount ||
                    uniqueBanCount >= maxUniqueBanCount)
                {
                    AssertError(true,
                        $"[{username}][{userId}] tried to log in an account where a machineid, uniqueid and/or ip has already been banned for " +
                        $"{machineBanCount}/{uniqueBanCount}/{ipBanCount} times. " +
                        $"(Max values: {maxMachineBanCount}/{maxUniqueBanCount}/{maxIpBanCount})");

                    if (ipBanCount >= maxIpBanCount)
                    {
                        result = 13; // rip.
                    }
                    else
                    {
                        Disconnect();
                        return;
                    }
                }
            }
            else
            {
                writeLoginInfo();
            }

            // Refresh the value
            isLoginOK = result <= 1;


            // -Banned- = 2
            // Deleted or Blocked = 3
            // Invalid Password = 4
            // Not Registered = 5
            // Sys Error = 6
            // Already online = 7
            // System error = 9
            // Too many requests = 10
            // Older than 20 = 11
            // Master cannot login on this IP = 13
            // Verify email = 16
            // Wrong gateway or change info = 17
            // Eula = 19

            var pack = new Packet(ServerMessages.CHECK_PASSWORD_RESULT);
            pack.WriteByte(result); //Login State
            pack.WriteByte(0); // nRegStatID
            pack.WriteInt(0); // nUseDay
            if (result <= 1)
            {
                pack.WriteInt(Player.ID);
                pack.WriteByte(Player.Gender);
                pack.WriteByte((byte)(Player.IsGM ? 1 : 0)); // Check more flags, 0x40/0x80?
                pack.WriteByte(0x01); //Country ID
                pack.WriteString(username);
                pack.WriteByte(0); // Purchase EXP
                // Wait is this actually here
                pack.WriteByte(0); // Chatblock Reason (1-5)
                pack.WriteLong(0); // Chat Unlock Date
            }
            else if (result == 2)
            {
                pack.WriteByte(banReason);
                pack.WriteLong(banExpire);
            }

            SendPacket(pack);

            if (!isLoginOK) Loaded = false;

            if (isLoginOK)
            {
                TryRegisterHackDetection();

                // Set online.

                RedisBackend.Instance.SetPlayerOnline(Player.ID, 1);

                if (crashLogTmp != null)
                {
                    var crashlogName = IP + "-" + username + ".txt";
                    FileWriter.WriteLine(Path.Combine("ClientCrashes", crashlogName), crashLogTmp);
                    crashLogTmp = null;
                    Server.Instance.ServerTraceDiscordReporter.Enqueue($"Saving crashlog to {crashlogName}");
                }

                AssertWarning(Player.IsGM == false && adminClient, $"[{username}] Logged in on an admin client while not being admin!!");

                Player.LoggedOn = true;
                Player.State = Player.Gender == 10 ? Player.LoginState.SetupGender : Player.LoginState.PinCheck;

                Program.MainForm.LogAppend($"Account {username} ({Player.ID}) logged on. Machine ID: {machineID}, Unique ID: {uniqueID}, IP: {IP}, Ban counts: {machineBanCount}/{uniqueBanCount}/{ipBanCount}");
                Program.MainForm.ChangeLoad(true);

                // Update database
                Server.Instance.UsersDatabase.RunQuery(
                    @"
                    UPDATE users SET 
                    last_login = NOW(), 
                    last_ip = @ip, 
                    last_machine_id = @machineId, 
                    last_unique_id = @uniqueId 
                    WHERE ID = @id",
                    "@id", Player.ID,
                    "@ip", IP,
                    "@machineId", machineID,
                    "@uniqueId", uniqueID
                );

                if (updateDBPass)
                {
                    Server.Instance.UsersDatabase.RunQuery(
                        "UPDATE users SET password = @password WHERE ID = @id",
                        "@id", Player.ID,
                        "@password", dbpass
                    );
                }
            }
            else if (result == 19)
            {
                Player.State = Player.LoginState.ConfirmEULA;
            }
        }

        public void OnSetGender(Packet packet)
        {
            if (AssertWarning(Player.State != Player.LoginState.SetupGender,
                "Tried to set gender while not in setup gender state")) return;

            if (packet.ReadBool() == false)
            {
                // 'back' to login
                BackToLogin();
                return;
            }

            bool isFemale = packet.ReadBool();

            Server.Instance.UsersDatabase.RunQuery(
                "UPDATE users SET gender = @gender WHERE ID = @id",
                "@id", Player.ID,
                "@gender", isFemale ? 1 : 0
            );

            Player.Gender = (byte)(isFemale ? 1 : 0);
            Player.State = Player.LoginState.PinCheck;

            var pack = new Packet(ServerMessages.SET_ACCOUNT_RESULT);
            pack.WriteBool(isFemale);
            pack.WriteByte(1);

            SendPacket(pack);
        }

        public void OnPinCheck(Packet packet)
        {
            if (AssertWarning(Player.State != Player.LoginState.PinCheck,
                "Tried to do a pin check while not in pin check state")) return;

            //PINs currently disabled. TODO when we update. Just send successful auth packet for now.
            Packet pack = new Packet(ServerMessages.PIN_OPERATION);
            pack.WriteByte(0);

            SendPacket(pack);

            Player.State = Player.LoginState.WorldSelect;
        }

        public void OnConfirmEULA(Packet packet)
        {
            if (AssertWarning(Player.State != Player.LoginState.ConfirmEULA, "Tried to confirm EULA while not in dialog")) return;

            if (packet.ReadBool())
            {
                Server.Instance.UsersDatabase.RunQuery(
                    "UPDATE users SET confirmed_eula = 1 WHERE ID = @id",
                    "@id", Player.ID
                );

                Packet pack = new Packet(ServerMessages.CONFIRM_EULA_RESULT);
                pack.WriteBool(true);
                SendPacket(pack);
            }

            BackToLogin();
        }

        private void BackToLogin()
        {
            Player.State = Player.LoginState.LoginScreen;
            Program.MainForm.ChangeLoad(false);

            Loaded = false;
            Player.LoggedOn = false;
            RedisBackend.Instance.RemovePlayerOnline(Player.ID);
        }
    }
}
