using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;
using WvsBeta.Game.Events.GMEvents;

namespace WvsBeta.Game
{
    public partial class frmMain : Form, IMainForm
    {
        int load = 0;
        public frmMain()
        {
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            System.Threading.Thread tr = new System.Threading.Thread(InitializeServer)
            {
                IsBackground = true // Prevents the server from hanging when you close it while it's loading.
            };

            tr.Start();
        }

        private void InitializeServer()
        {
            try
            {
                Server.Init(Program.IMGFilename);
                DataProvider.Load();

                for (var reconnects = 0; reconnects < 8; reconnects++)
                {
                    for (var timeouts = 0; timeouts < 10; timeouts++)
                    {
                        if (Server.Instance.ID != 0xFF) break;
                        System.Threading.Thread.Sleep(100);
                    }
                    if (Server.Instance.ID != 0xFF) break;

                    Server.Instance.ConnectToCenter();
                    System.Threading.Thread.Sleep(500);
                }


                if (Server.Instance.ID == 0xFF)
                {
                    Environment.Exit(1);
                }

                this.Invoke((MethodInvoker)delegate
                {
                    this.txtLoad.Text = "0";
                    this.Text += " Channel " + (Server.Instance.ID + 1) + " (" + Program.IMGFilename + ")";
                });

                MasterThread.RepeatingAction.Start("Map Checker", Server.Instance.CheckMaps, 0, 1000);

                LogAppend("Server successfully booted!");

                MasterThread.RepeatingAction.Start(
                    "MasterThread Stats for form",
                    x =>
                    {
                        try
                        {
                            this.Invoke((MethodInvoker)delegate
                           {
                               var mt = MasterThread.Instance;
                               lblMasterThreadStats.Text =
                                   $"MT Queue: {mt.CurrentCallbackQueueLength}; " +
                                   $"RA count: {mt.RegisteredRepeatingActions}; " +
                                   $"Ping conns: {Pinger.CurrentLoggingConnections}";
                           });
                        }
                        catch { }
                    },
                    0,
                    2000
                );


                if (Server.Instance.InMigration)
                {
                    // Tell the other server to start migrating...
                    var pw = new Packet(ISClientMessages.ServerMigrationUpdate);
                    pw.WriteByte((byte)ServerMigrationStatus.StartMigration);
                    Server.Instance.CenterConnection.SendPacket(pw);
                }

                Pinger.Init(x => Program.MainForm.LogAppend(x), x => Program.MainForm.LogAppend(x));
            }
            catch (Exception ex)
            {
                Program.LogFile.WriteLine("Got exception @ frmMain::InitServer : {0}", ex.ToString());
                MessageBox.Show(string.Format("[{0}][GAME SERVER] Got exception @ frmMain::InitServer:\r\n {1}", DateTime.Now.ToString(), ex.ToString()));
                Environment.Exit(5);
            }
        }

        public void ChangeLoad(bool up)
        {
            if (this.Visible == false) return;
            if (up)
            {
                ++load;
                //LogAppend(string.Format("[{0}] Received a connection! The server now has {1} connections.", DateTime.Now.ToString(), load));
            }
            else
            {
                --load;
                //LogAppend(string.Format("[{0}] Lost a connection! The server now has {1} connections.", DateTime.Now.ToString(), load));
            }

            txtLoad.Invoke((MethodInvoker)delegate
            {
                txtLoad.Text = load.ToString();
            });

            Server.Instance.CenterConnection.SendUpdateConnections(load);
        }

        private static bool alreadyShuttingDown = false;
        private static bool forceShutDown = false;

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (forceShutDown) return;

            e.Cancel = true;
            Shutdown();
        }
        
        public void Shutdown()
        {
            if (alreadyShuttingDown)
            {
                return;
            }
            alreadyShuttingDown = true;
            
            Program.MainForm.LogAppend("Getting rid of players");

            int timeout = 10;

            var startTime = MasterThread.CurrentTime;
            MasterThread.RepeatingAction ra = null; // Prevent error
            ra = new MasterThread.RepeatingAction(
                "Client DC Thread",
                (date) =>
                {
                    var isTimeout = (date - startTime) > timeout * 1000;
                    if (Server.Instance.PlayerList.Count == 0 || isTimeout)
                    {
                        var queueLen = MasterThread.Instance.CurrentCallbackQueueLength;
                        var waiting = (long) Math.Max(700, Math.Min(3000, queueLen * 100));
                        LogAppend($"Preparing shutdown... Timeout? {isTimeout} Queue size: {queueLen}, waiting for {waiting} millis");

                        MasterThread.RepeatingAction.Start(
                            "Server finalizing and shutdown thread",
                            (date2) =>
                            {
                                forceShutDown = true;
                                Server.Instance.CenterConnection?.Disconnect();
                                MasterThread.Instance.Stop = true;
                                Environment.Exit(1);
                            },
                            waiting,
                            0
                        );
                        MasterThread.Instance.RemoveRepeatingAction(ra);
                    }
                    else
                    {
                        Server.Instance.PlayerList.ForEach(x =>
                        {
                            if (x.Value.Character == null) return;
                            try
                            {
                                x.Value.Character.CleanupInstances();
                                x.Value.Socket.Disconnect();
                            }
                            catch { }
                        });
                    }
                },
                0,
                100
            );

            MasterThread.Instance.AddRepeatingAction(ra);
            return;
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            MasterThread.Instance.AddCallback((time) =>
            {
                var lvis = new List<ListViewItem>();
                foreach (var kvp in Server.Instance.PlayerList)
                {
                    var player = kvp.Value;
                    var character = player.Character;
                    if (character == null) return;

                    lvis.Add(new ListViewItem(new[]
                    {
                        character.UserID.ToString(),
                        character.ID.ToString(),
                        character.Name + (character.IsAFK ? " (AFK)" : ""),
                        character.MapID.ToString(),
                        (player.Socket.PingMS / 2).ToString() + " ms"
                    }));
                }

                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        lvPlayers.BeginUpdate();
                        lvPlayers.Items.Clear();
                        lvPlayers.Items.AddRange(lvis.ToArray());
                        lvPlayers.EndUpdate();
                    });
                }
                catch { }
            }, "btnRefresh");
        }

        
        public void LogAppend(string what)
        {
            var keys = log4net.ThreadContext.Properties.GetKeys() ?? new string[0] { };
            var copy = keys.Select(x => new Tuple<string, object>(x, log4net.ThreadContext.Properties[x])).ToArray();
            MasterThread.Instance.AddCallback((date) =>
            {
                copy.ForEach(x => log4net.ThreadContext.Properties[x.Item1] = x.Item2);
                txtLog.AddLine(what);
                LogToFile(what);
                copy.ForEach(x => log4net.ThreadContext.Properties.Remove(x.Item1));
            }, "LogAppend");
        }

        public void LogAppend(string pFormat, params object[] pParams)
        {
            LogAppend(string.Format(pFormat, pParams));
        }
        
        public void LogDebug(string pFormat, params object[] pParams)
        {
#if DEBUG
            LogAppend(string.Format(pFormat, pParams));
#endif
        }

        public void LogToFile(string what)
        {
            Program.LogFile.WriteLine(what);
        }

        private void lblMasterThreadStats_Click(object sender, EventArgs e)
        {

        }
    }
}