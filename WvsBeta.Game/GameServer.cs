using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;
using WvsBeta.Database;
using WvsBeta.Game.GameObjects;
using WvsBeta.SharedDataProvider;

namespace WvsBeta.Game
{
    public class Server
    {
        public static bool Tespia { get; private set; }
        public static Server Instance { get; private set; }

        public Rand32 Randomizer { get; set; } = new Rand32();
        public LoopingID MiniRoomIDs { get; set; } = new LoopingID();
        public LoopingID PartyIDs { get; set; } = new LoopingID();
        public LoopingID MatchCardIDs { get; set; } = new LoopingID();

        public double RateMobEXP = 1.0d;
        public double RateMesoAmount = 1.0d;
        public double RateDropChance = 1.0d;

        public byte ID { get; set; }
        public bool InMigration { get; set; }
        public bool IsNewServerInMigration { get; set; }
        public bool CenterMigration { get; set; }
        public string Name { get; set; }
        public string WorldName { get; set; }
        public byte WorldID { get; set; }

        public int CenterPort { get; set; }
        public IPAddress CenterIP { get; set; }

        public ushort Port { get; set; }
        public IPAddress PublicIP { get; set; }
        public IPAddress PrivateIP { get; set; }

        public CenterSocket CenterConnection { get; set; }

        public int GetOnlineId() => RedisBackend.GetOnlineId(WorldID, ID);

        public bool Initialized { get; private set; }

        private GameAcceptor GameAcceptor { get; set; }
        public MySQL_Connection CharacterDatabase { get; private set; }

        public Dictionary<int, Tuple<Packet, long>> CCIngPlayerList { get; } = new Dictionary<int, Tuple<Packet, long>>();
        public ConcurrentDictionary<string, Player> PlayerList { get; } = new ConcurrentDictionary<string, Player>();
        public Dictionary<int, Character> CharacterList { get; } = new Dictionary<int, Character>();
        public HashSet<Character> StaffCharacters { get; } = new HashSet<Character>();

        public Dictionary<int, (string reason, string name, byte level, Character.BanReasons banReason, long time)> DelayedBanRecords { get; } = new Dictionary<int, (string, string, byte, Character.BanReasons, long)>();

        public DiscordReporter BanDiscordReporter { get; private set; }
        public DiscordReporter ServerTraceDiscordReporter { get; private set; }
        public DiscordReporter MutebanDiscordReporter { get; private set; }

        private Dictionary<string, INpcScript> _availableNPCScripts { get; } = new Dictionary<string, INpcScript>();

        public string ScrollingHeader { get; private set; }

        public void SetScrollingHeader(string newText)
        {
            ScrollingHeader = newText;
            Program.MainForm.LogAppend("Updating scrolling header to: {0}", ScrollingHeader);

            MessagePacket.SendText(MessagePacket.MessageTypes.Header, ScrollingHeader, null, MessagePacket.MessageMode.ToChannel);
        }

        public void LogToLogfile(string what)
        {
            Program.LogFile.WriteLine(what);
        }

        public void AddDelayedBanRecord(Character chr, string reason, Character.BanReasons banReason, int extraDelay)
        {
            // Only enqueue when we haven't recorded it yet, otherwise you would
            // be able to extend the A/B delay.
            if (DelayedBanRecords.ContainsKey(chr.UserID)) return;


            Character.HackLog.Info(new Character.PermaBanLogRecord
            {
                reason = reason
            });
            var seconds = Rand32.NextBetween(3, 10) + extraDelay;
            DelayedBanRecords[chr.UserID] = (reason, chr.Name, chr.Level, banReason, MasterThread.CurrentTime + (seconds * 1000));

            var str = $"Enqueued delayed permban for userid {chr.UserID}, charname {chr.Name}, level {chr.Level}, reason ({banReason}) {reason}, map {chr.MapID} in {seconds} seconds...";
            BanDiscordReporter.Enqueue(str);

            MessagePacket.SendNoticeGMs(
                str,
                MessagePacket.MessageTypes.Notice
            );
        }

        public void CheckMaps(long pNow)
        {
            DataProvider.Maps.ForEach(x => x.Value.MapTimer(pNow));
        }

        public INpcScript TryGetOrCompileScript(string scriptName, Action<string> errorHandlerFnc)
        {
            if (_availableNPCScripts.TryGetValue(scriptName, out INpcScript ret)) return ret;

            var scriptUri = GetScriptFilename(scriptName);

            if (scriptUri == null) return null;

            return ForceCompileScriptfile(scriptUri, errorHandlerFnc);
        }

        public string GetScriptFilename(string scriptName)
        {
            var scriptsDir = Path.Combine(Environment.CurrentDirectory, "..", "DataSvr", "Scripts");

            string filename = Path.Combine(scriptsDir, scriptName + ".s");
            if (!File.Exists(filename)) filename = Path.Combine(scriptsDir, scriptName + ".cs");
            if (!File.Exists(filename)) return null;
            return filename;
        }

        public INpcScript ForceCompileScriptfile(string filename, Action<string> errorHandlerFnc)
        {
            var fi = new FileInfo(filename);
            var results = Scripting.CompileScript(filename);
            if (results.Errors.Count > 0)
            {
                errorHandlerFnc?.Invoke(Path.GetFileName(filename));

                Program.MainForm.LogAppend($"Couldn't compile the file ({filename}) correctly:");
                foreach (CompilerError error in results.Errors)
                {
                    Program.MainForm.LogAppend(
                        $"File {filename}, Line {error.Line}, Column {error.Column}: {error.ErrorText}");
                }
                return null;
            }


            var ret = (INpcScript)Scripting.FindInterface(results.CompiledAssembly, "INpcScript");
            string savename = fi.Name.Replace(".s", "").Replace(".cs", "");
            _availableNPCScripts[savename] = ret;
            return ret;
        }

        public void AddPlayer(Player player)
        {
            string hash;
            do
            {
                hash = Cryptos.GetNewSessionHash();
            } while (PlayerList.ContainsKey(hash));
            PlayerList.TryAdd(hash, player);
            player.SessionHash = hash;
        }

        public void RemovePlayer(string hash)
        {
            for (var i = 0; i < 3; i++)
            {
                if (PlayerList.TryRemove(hash, out Player derp)) return;
            }
            Program.MainForm.LogAppend("Unable to remove player with hash {0}", hash);
        }

        public Character GetCharacter(int ID)
        {
            return CharacterList.TryGetValue(ID, out Character ret) ? ret : null;
        }


        public Character GetCharacter(string name)
        {
            name = name.ToLowerInvariant();
            return (from kvp in CharacterList where kvp.Value != null && kvp.Value.Name.ToLowerInvariant() == name select kvp.Value).FirstOrDefault();
        }

        public bool IsPlayer(string hash)
        {
            return PlayerList.ContainsKey(hash);
        }

        public Player GetPlayer(string hash)
        {
            return PlayerList.TryGetValue(hash, out Player ret) ? ret : null;
        }

        public static void Init(string configFile)
        {
            Instance = new Server()
            {
                Name = configFile,
                ID = 0xFF
            };
            Instance.Load();
        }

        private string GetConfigPath(string filename) =>
            Path.Combine(Environment.CurrentDirectory, "..", "DataSvr", filename);

        void Load()
        {
            Initialized = false;
            LoadConfig(new ConfigReader(GetConfigPath(Name + ".img")));
            LoadDBConfig(GetConfigPath("Database.img"));

            ConnectToCenter();

            Initialized = true;

            MasterThread.RepeatingAction.Start("RemoveNotConnectingPlayers",
                curTime =>
                {
                    var tmp = CCIngPlayerList.ToArray();
                    foreach (var elem in tmp)
                    {
                        if ((elem.Value.Item2 - curTime) > 10000)
                        {
                            CCIngPlayerList.Remove(elem.Key);
                        }
                    }
                },
                0,
                5000
            );

            MasterThread.RepeatingAction.Start("Delayed Ban Processor",
                curTime =>
                {
                    var tmp = DelayedBanRecords.ToList();
                    foreach (var keyValuePair in tmp)
                    {
                        var value = keyValuePair.Value;
                        var userid = keyValuePair.Key;
                        if (value.time <= curTime)
                        {
                            CenterConnection.KickUser(userid);
                            CharacterDatabase.PermaBan(userid, (byte)value.banReason, "AB-" + Name, value.reason);

                            var (maxMachineBanCount, maxUniqueBanCount, maxIpBanCount) = CharacterDatabase.GetUserBanRecordLimit(userid);
                            var (machineBanCount, uniqueBanCount, ipBanCount) = CharacterDatabase.GetUserBanRecord(userid);

                            var str = $"Delayed permaban for userid {userid}, charname {value.name}, level {value.level}, reason {value.reason}. Ban counts: {machineBanCount}/{uniqueBanCount}/{ipBanCount} of {maxMachineBanCount}/{maxUniqueBanCount}/{maxIpBanCount}.";
                            if (uniqueBanCount >= maxUniqueBanCount ||
                                ipBanCount >= maxIpBanCount)
                            {
                                str += " Reached limits, so new accounts are useless.";
                            }

                            BanDiscordReporter.Enqueue(str);

                            MessagePacket.SendNoticeGMs(
                                str,
                                MessagePacket.MessageTypes.Notice
                            );

                            DelayedBanRecords.Remove(userid);
                        }
                    }
                },
                0,
                1000
            );

            CharacterDatabase.SetupPinger(MasterThread.Instance);

            ContinentMan.Init();

            DiscordReporter.Username = Program.IMGFilename;
            BanDiscordReporter = new DiscordReporter(DiscordReporter.BanLogURL);

            ServerTraceDiscordReporter = new DiscordReporter(DiscordReporter.ServerTraceURL);

            MutebanDiscordReporter = new DiscordReporter(
                "discord muteban discord report url"
            );

            Handlers.Commands.MainCommandHandler.ReloadCommands();
        }

        public void ConnectToCenter()
        {
            if (CenterConnection?.Disconnected == false) return;
            CenterConnection = new CenterSocket();
        }

        private void LoadDBConfig(string configFile)
        {
            var lines = File.ReadLines(configFile)
                .Select(l => l.Split(' '))
                .Select(p => p.Length == 2 ? "" : p[2])
                .ToList();
            string Username = lines[0];
            string Password = lines[1];
            string Database = lines[2];
            string Host = lines[3];

            CharacterDatabase = new MySQL_Connection(MasterThread.Instance, Username, Password, Database, Host);
            BaseCharacterInventory.Connection = CharacterDatabase;
            CharacterCashItems.Connection = CharacterDatabase;
        }

        private void LoadConfig(ConfigReader reader)
        {
            Port = reader["port"].GetUShort();
            WorldID = reader["gameWorldId"].GetByte();

            CharacterCashItems.WorldID = WorldID;
            log4net.GlobalContext.Properties["WorldID"] = WorldID;

            PublicIP = IPAddress.Parse(reader["PublicIP"].GetString());
            PrivateIP = IPAddress.Parse(reader["PrivateIP"].GetString());

            CenterIP = IPAddress.Parse(reader["center"]["ip"].GetString());
            CenterPort = reader["center"]["port"].GetUShort();
            WorldName = reader["center"]["worldName"].GetString();

            Tespia = reader["tespia"]?.GetBool() ?? false;

            string tmpHeader = reader["scrollingHeader"]?.GetString() ?? "";
            if (tmpHeader == "EMPTY")
            {
                tmpHeader = "";
            }

            ScrollingHeader = tmpHeader;

            RedisBackend.Init(reader);
        }

        public void LoadFieldSet()
        {
            var reader = new ConfigReader(GetConfigPath("FieldSet.img"));
            foreach (var node in reader.RootNode)
            {
                new FieldSet(node);
            }
        }

        public void StartListening()
        {
            Program.MainForm.LogAppend($"Starting to listen on port {Port}");
            GameAcceptor = new GameAcceptor();
        }

        public void StopListening()
        {
            Program.MainForm.LogAppend($"Stopped listening on port {Port}");
            GameAcceptor?.Stop();
            GameAcceptor = null;
        }
    }
}