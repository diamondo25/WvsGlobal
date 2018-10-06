using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using WvsBeta.Center.DBAccessor;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;
using WvsBeta.Database;

namespace WvsBeta.Center
{

    public class CenterServer
    {
        public static CenterServer Instance { get; set; }

        public string ConfigName { get; private set; }
        public bool InMigration { get; set; }
        public static bool Tespia { get; private set; }

        public ushort Port { get; set; }
        public ushort AdminPort { get; set; }
        public ushort CTCPort => (ushort)(Port + 10000);
        public IPAddress PrivateIP { get; set; }
        public Dictionary<string, LocalServer> LocalServers { get; } = new Dictionary<string, LocalServer>();
        public WorldServer World { get; set; }
        public MySQL_Connection CharacterDatabase { get; set; }

        public ServerConnectionAcceptor ConnectionAcceptor { get; set; }
        public CenterToCenterAcceptor CenterToCenterAcceptor { get; private set; }
        public CenterToCenterConnection CenterToCenterConnection { get; set; }

        public List<Character> CharacterStore { get; } = new List<Character>();
        public List<Messenger> MessengerRooms { get; } = new List<Messenger>();

        public void LogToLogfile(string what)
        {
            Program.LogFile.WriteLine(what);
        }

        public Character FindCharacter(string name, bool onlyOnline = true)
        {
            var chr = CharacterStore.Find(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            if (onlyOnline && (chr?.IsOnline ?? false) == false) return null;
            return chr;
        }

        public Character FindCharacter(int id, bool onlyOnline = true)
        {
            var chr = CharacterStore.Find(c => c.ID == id);
            if (onlyOnline && (chr?.IsOnline ?? false) == false) return null;
            return chr;
        }

        public bool IsOnline(CharacterBase pCharacter) => pCharacter?.IsOnline ?? false;
        public bool IsOnline(int CharacterID) => IsOnline(FindCharacter(CharacterID));

        public Character AddCharacter(string name, int id, byte channel, short job, byte level, byte gm)
        {
            var chr = FindCharacter(id, false);
            if (chr == null)
            {
                chr = new Character()
                {
                    Name = name,
                    ID = id,
                    isCCing = false,
                    GMLevel = gm,
                    IsOnline = true,
                };

                CharacterStore.Add(chr);
            }
            else if (chr.isCCing)
            {
                chr.isCCing = false;
            }

            chr.ChannelID = channel;
            chr.IsOnline = true;
            // Resetting party ID to update it on the server
            chr.PartyID = chr.PartyID;

            if (chr.FriendsList == null)
            {
                chr.FriendsList = BuddyList.LoadBuddyList(id, name);
            }

            chr.GMLevel = gm;
            Program.MainForm.LogDebug(chr.Name + " Staff? " + chr.IsGM + " GM Level: " + chr.GMLevel);
            chr.Job = job;
            chr.Level = level;

            return chr;
        }


        public static void Init(string configFile)
        {
            Instance = new CenterServer()
            {
                ConfigName = configFile
            };
            Instance.Load();
        }

        public void Load()
        {
            Program.MainForm.LogAppend("Reading Config File... ", false);
            LoadConfig(Path.Combine(Environment.CurrentDirectory, "..", "DataSvr", ConfigName + ".img"));
            LoadDBConfig(Path.Combine(Environment.CurrentDirectory, "..", "DataSvr", "Database.img"));
            ReloadEvents();
            Program.MainForm.UpdateServerList();

            Program.MainForm.LogAppend(" Done!", false);

            Program.MainForm.LogAppend("Starting to patch... ", false);
            DataBasePatcher.StartPatching(CharacterDatabase, Path.Combine(Application.StartupPath, "evolutions", "center"), "center");
            Program.MainForm.LogAppend(" Done!", false);


            using (var reader = CharacterDatabase.RunQuery(
                    "SELECT private_ip FROM servers WHERE configname = 'Center' AND world_id = " + World.ID
                ) as MySqlDataReader)
            {
                if (reader != null && reader.Read())
                {
                    // Server exists, try to migrate
                    Program.MainForm.LogAppend("Starting migration...");
                    var privateIp = reader.GetString("private_ip");
                    reader.Close(); // Close reader here to prevent next updates to error

                    try
                    {
                        bool wasConnected = false;
                        CenterToCenterConnection = new CenterToCenterConnection(privateIp, CTCPort);
                        for (var i = 0; i < 10; i++)
                        {
                            System.Threading.Thread.Sleep(100);
                            if (CenterToCenterConnection.Disconnected == false)
                            {
                                wasConnected = true;
                                break;
                            }
                        }

                        if (!wasConnected)
                        {
                            // Timeout
                            CenterToCenterConnection.PreventConnectFromSucceeding = true;
                            CenterToCenterConnection = null;
                            Program.MainForm.LogAppend("Not able to migrate as server is not accessible.");
                            StartCTCAcceptor();
                            StartListening();
                        }
                        else
                        {
                            Program.MainForm.LogAppend("Connected to CTC acceptor");
                            InMigration = false;
                            var pw = new Packet(ISServerMessages.ServerMigrationUpdate);
                            pw.WriteByte((byte)ServerMigrationStatus.StartMigration);
                            CenterToCenterConnection.SendPacket(pw);
                        }
                    }
                    catch (Exception ex)
                    {
                        Program.MainForm.LogAppend("Migration failed! {0}", ex);
                        // It failed.
                        StartCTCAcceptor();
                        StartListening();
                    }
                }
                else
                {
                    StartCTCAcceptor();
                    StartListening();
                }
            }

            CharacterDatabase.SetupPinger(MasterThread.Instance);

            MasterThread.RepeatingAction.Start("WorldServer event checker", World.CheckForEvents, 0, 2000);
        }

        public void StartCTCAcceptor()
        {
            Program.MainForm.LogAppend("Starting CTC acceptor on port {0}", CTCPort);
            CenterToCenterAcceptor = new CenterToCenterAcceptor(CTCPort);
            CharacterDatabase.RunQuery(
                "DELETE FROM servers WHERE configname = 'Center' AND world_id = @worldId; " +
                "INSERT INTO servers VALUES ('Center', @worldId, @ip);",
                "@worldId", World.ID,
                "@ip", PrivateIP
            );
        }

        public void StopListening()
        {
            ConnectionAcceptor?.Stop();
            ConnectionAcceptor = null;
        }

        public void StartListening()
        {
            ConnectionAcceptor = new ServerConnectionAcceptor();
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
            CharacterDBAccessor.InitializeDB(CharacterDatabase);
        }

        private void LoadConfig(string configFile)
        {
            ConfigReader reader = new ConfigReader(configFile);
            Port = reader["port"].GetUShort();
            AdminPort = reader["adminPort"].GetUShort();
            PrivateIP = IPAddress.Parse(reader["PrivateIP"]?.GetString() ?? "127.0.0.1");
            World = new WorldServer(reader["gameWorldId"].GetByte());

            World.UserWarning = reader["userWarning"]?.GetInt() ?? 400; // 11000
            World.UserLimit = reader["userLimit"]?.GetInt() ?? 600; // 12000

            if (World.UserLimit < World.UserWarning)
            {
                var tmp = World.UserLimit;
                World.UserLimit = World.UserWarning;
                World.UserWarning = tmp;
            }

            Tespia = reader["tespia"]?.GetBool() ?? false;

            LocalServer ls;
            LocalServerType lst;
            foreach (var serverCategory in reader.RootNode.Where(x => x.SubNodes != null))
            {
                switch (serverCategory.Name)
                {
                    case "login": lst = LocalServerType.Login; break;
                    case "game": lst = LocalServerType.Game; break;
                    case "shop": lst = LocalServerType.Shop; break;
                    case "mapgen": lst = LocalServerType.MapGen; break;
                    case "claim": lst = LocalServerType.Claim; break;
                    case "itc": lst = LocalServerType.ITC; break;
                    default: lst = LocalServerType.Unk; break;
                }

                if (lst == LocalServerType.Unk)
                {
                    System.Windows.Forms.MessageBox.Show("Found unparsable block in center config file: " + serverCategory.Name);
                }
                else if (lst == LocalServerType.Claim)
                {
                    ls = new LocalServer()
                    {
                        Name = "claim",
                        Port = serverCategory["port"].GetUShort(),
                        PublicIP = IPAddress.Parse(serverCategory["PublicIP"].GetString()),
                        PrivateIP = IPAddress.Parse(serverCategory["PrivateIP"].GetString()),
                        Type = lst
                    };
                    LocalServers.Add("claim", ls);
                }
                else
                {
                    byte channelId = 0;
                    foreach (var serverConfigBlock in serverCategory)
                    {
                        ls = new LocalServer()
                        {
                            Name = serverConfigBlock.Name,
                            Port = serverConfigBlock["port"].GetUShort(),
                            PublicIP = IPAddress.Parse(serverConfigBlock["PublicIP"].GetString()),
                            PrivateIP = IPAddress.Parse(serverConfigBlock["PrivateIP"].GetString()),
                            RateMobEXP = serverConfigBlock["EXP_Rate"]?.GetDouble() ?? 1.0,
                            RateMesoAmount = serverConfigBlock["MESO_Rate"]?.GetDouble() ?? 1.0,
                            RateDropChance = serverConfigBlock["DROP_Rate"]?.GetDouble() ?? 1.0,
                            Type = lst,
                        };

                        ls.SetRates(ls.RateMobEXP, ls.RateDropChance, ls.RateMesoAmount, false, true);

                        LocalServers.Add(ls.Name, ls);
                        if (lst == LocalServerType.Game)
                        {
                            World.GameServers.Add(channelId, ls);
                            ls.ChannelID = channelId;
                        }
                        else if (lst == LocalServerType.Shop)
                        {
                            World.ShopServers.Add(channelId, ls);
                            ls.ChannelID = channelId;
                        }
                        channelId++;
                    }

                    if (lst == LocalServerType.Game)
                        World.Channels = channelId;
                }
            }


            RedisBackend.Init(reader);
        }

        public void ReloadEvents()
        {
            var filename = Path.Combine(Environment.CurrentDirectory, "..", "DataSvr", "Events.img");
            if (File.Exists(filename) == false)
            {
                Program.MainForm.LogAppend("Unable to load events; Events.img not found.");
                return;
            }
            World.LoadEvents(new ConfigReader(filename));
        }


        public void SendPacketToServer(Packet pPacket, byte pChannelID = 0xFF)
        {
            if (pChannelID == 0xFF)
            {
                World.SendPacketToEveryGameserver(pPacket);
            }
            else
            {
                if (!World.GameServers.TryGetValue(pChannelID, out LocalServer ls))
                {
                    Program.MainForm.LogAppend("Cannot send packet (channel not found: " + pChannelID + ")");
                }
                else if (!ls.Connected)
                {
                    Program.MainForm.LogAppend("Cannot send packet (channel offline: " + pChannelID + ")");
                }
                else
                {
                    ls.Connection.SendPacket(pPacket);
                }
            }
        }

    }
}