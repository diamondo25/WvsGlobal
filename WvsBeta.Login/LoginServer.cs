using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;
using WvsBeta.Database;
using System.Linq;

namespace WvsBeta.Login
{
    class Server
    {
        public static Server Instance { get; private set; }
        public static bool Tespia { get; private set; }

        public ushort Port { get; set; }
        public ushort AdminPort { get; set; }
        public ushort LTLPort => (ushort) (Port + 10000);
        public IPAddress PublicIP { get; set; }
        public IPAddress PrivateIP { get; set; }
        public bool RequiresEULA { get; set; }
        public Dictionary<byte, Center> Worlds = new Dictionary<byte, Center>();
        public string Name { get; set; }
        public bool InMigration { get; set; }
        public Dictionary<short, short> PatchNextVersion { get; } = new Dictionary<short, short>();
        public int DataChecksum { get; private set; }
        public short CurrentPatchVersion { get; private set; }

        private LoginAcceptor LoginAcceptor { get; set; }
        public LoginToLoginAcceptor LoginToLoginAcceptor { get; set; }
        public LoginToLoginConnection LoginToLoginConnection { get; set; }

        public DiscordReporter ServerTraceDiscordReporter { get; private set; }

        public MySQL_Connection UsersDatabase { get; private set; }

        private ConcurrentDictionary<string, Player> PlayerList { get; } = new ConcurrentDictionary<string,Player>();
        
        public void LogToLogfile(string what)
        {
            Program.LogFile.Write(what);
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
            PlayerList.TryRemove(hash, out Player tmp);
        }

        public bool IsPlayer(string hash)
        {
            return PlayerList.ContainsKey(hash);
        }

        public Player GetPlayer(string hash)
        {
            if (PlayerList.TryGetValue(hash, out Player player)) return player;
            return null;
        }

        public bool GetWorld(byte worldId, out Center world, bool onlyConnected = true)
        {
            if (!Worlds.TryGetValue(worldId, out Center tmp) || (onlyConnected && !tmp.IsConnected))
            {
                world = null;
                return false;
            }
            world = tmp;
            return true;
        }

        public static void Init(string configFile)
        {
            Instance = new Server()
            {
                Name = configFile
            };
            Instance.Load();
        }

        private string ServerConfigFile => Path.Combine(Environment.CurrentDirectory, "..", "DataSvr", Name + ".img");


        public void Load()
        {
            Program.MainForm.LogAppend("Reading Config File... ", false);
            LoadConfig(ServerConfigFile);
            LoadClientPatchData(ServerConfigFile);
            LoadDBConfig(Path.Combine(Environment.CurrentDirectory, "..", "DataSvr", "Database.img"));
            Program.MainForm.LogAppend(" Done!", false);
            
            Program.MainForm.LogAppend("Starting to patch... ", false);
            DataBasePatcher.StartPatching(UsersDatabase, Path.Combine(Application.StartupPath, "evolutions", "login"), "login");

            Program.MainForm.LogAppend(" Done!", false);

            MasterThread.RepeatingAction.Start("Center Reconnect Timer", time =>
            {
                foreach (var kvp in Worlds)
                {
                    if (kvp.Value.IsConnected) continue;
                    try
                    {
                        kvp.Value.Connect();
                    }
                    catch { }
                }
            }, 0, 5000);


            using (var reader = UsersDatabase.RunQuery(
                "SELECT private_ip FROM servers WHERE configname = '" + Name + "' AND world_id = 0"
            ) as MySqlDataReader)
            {
                if (reader != null && reader.Read())
                {
                    // Server exists, try to migrate
                    var privateIp = reader.GetString("private_ip");
                    Program.MainForm.LogAppend("Starting migration... {0}:{1}", privateIp, LTLPort);
                    reader.Close();

                    try
                    {
                        bool wasConnected = false;
                        LoginToLoginConnection = new LoginToLoginConnection(privateIp, LTLPort);
                        for (var i = 0; i < 10; i++)
                        {
                            System.Threading.Thread.Sleep(100);
                            if (LoginToLoginConnection.Disconnected == false)
                            {
                                wasConnected = true;
                                break;
                            }
                        }

                        if (!wasConnected)
                        {
                            LoginToLoginConnection.PreventConnectFromSucceeding = true;
                            Program.MainForm.LogAppend("Not able to migrate as server is not accessible.");
                            StartLTLAcceptor();
                            StartListening();
                        }
                        else
                        {
                            Program.MainForm.LogAppend("Connected to LTL acceptor");
                            InMigration = false;
                            var pw = new Packet(ISServerMessages.ServerMigrationUpdate);
                            pw.WriteByte((byte)ServerMigrationStatus.StartMigration);
                            LoginToLoginConnection.SendPacket(pw);
                        }
                    }
                    catch (Exception ex)
                    {
                        Program.MainForm.LogAppend("Migration failed! {0}", ex);
                        // It failed.
                        StartLTLAcceptor();
                        StartListening();
                    }
                }
                else
                {
                    StartLTLAcceptor();
                    StartListening();
                }
            }

            UsersDatabase.SetupPinger(MasterThread.Instance);


            DiscordReporter.Username = Name;
            ServerTraceDiscordReporter = new DiscordReporter(DiscordReporter.ServerTraceURL);
        }

        public void StartLTLAcceptor()
        {
            Program.MainForm.LogAppend("Starting LTL acceptor on port {0}", LTLPort);
            LoginToLoginAcceptor = new LoginToLoginAcceptor(LTLPort);
            UsersDatabase.RunQuery(
                "DELETE FROM servers WHERE configname = '" + Name + "' AND world_id = 0; " +
                "INSERT INTO servers VALUES ('" + Name + "', 0, '" + PrivateIP + "');"
            );
        }

        public void StopListening()
        {
            LoginAcceptor?.Stop();
            LoginAcceptor = null;
        }

        public void StartListening()
        {
            LoginAcceptor = new LoginAcceptor();
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

            UsersDatabase = new MySQL_Connection(MasterThread.Instance, Username, Password, Database, Host);
        }

        private void LoadConfig(string configFile)
        {
            ConfigReader reader = new ConfigReader(configFile);
            Port = reader["port"].GetUShort();
            AdminPort = reader["adminPort"].GetUShort();
            PublicIP = IPAddress.Parse(reader["PublicIP"].GetString());
            PrivateIP = IPAddress.Parse(reader["PrivateIP"].GetString());

            RequiresEULA = reader["requiresEULA"]?.GetBool() ?? false;
            Tespia = reader["tespia"]?.GetBool() ?? false;

            foreach (var worldConfig in reader["center"])
            {
                var center = new Center
                {
                    Channels = worldConfig["channelNo"].GetByte(),
                    ID = worldConfig["world"].GetByte(),
                    Port = worldConfig["port"].GetUShort(),
                    IP = IPAddress.Parse(worldConfig["ip"].GetString()),
                    AdultWorld = worldConfig["adult"]?.GetBool() ?? false,
                    EventDescription = worldConfig["eventDesc"]?.GetString() ?? "",
                    BlockCharacterCreation = worldConfig["BlockCharCreation"]?.GetBool() ?? false,
                    State = worldConfig["worldState"]?.GetByte() ?? 0,
                    Name = worldConfig.Name,
                };
                center.UserNo = new int[center.Channels];

                Worlds.Add(center.ID, center);
            }

            RedisBackend.Init(reader);
            WzReader.Load();
        }

        private void LoadClientPatchData(string configFile)
        {
            ConfigReader reader = new ConfigReader(configFile);
            PatchNextVersion.Clear();
            CurrentPatchVersion = 0;
            DataChecksum = reader["dataChecksum"]?.GetInt() ?? 0;

            var versionAndChecksumNode = reader["versionUpdates"];
            if (versionAndChecksumNode != null)
            {
                foreach (var node in versionAndChecksumNode)
                {
                    short fromVersion = short.Parse(node.Name);
                    short toVersion = node.GetShort();
                    PatchNextVersion.Add(fromVersion, toVersion);
                    CurrentPatchVersion = Math.Max(CurrentPatchVersion, toVersion);
                    Program.MainForm.LogAppend("Loaded patch {0} -> {1}", fromVersion, toVersion);
                }
            }

            Program.MainForm.LogAppend("Current patch version: {0}", CurrentPatchVersion);
            Program.MainForm.LogAppend("Data checksum: {0} 0x{0:X8}", DataChecksum);
        }
    }
}
