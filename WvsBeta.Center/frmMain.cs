using System;
using System.Threading;
using System.Windows.Forms;
using log4net;
using WvsBeta.Center.Properties;
using WvsBeta.Common;

namespace WvsBeta.Center
{
    public partial class frmMain : Form
    {
        private static ILog _onlinePlayerLog = LogManager.GetLogger("OnlinePlayers");
        public struct OnlinePlayerCount
        {
            public string serverName { get; set; }
            public int count { get; set; }
        }

        private int _totalConnections;

        public frmMain()
        {
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            var tr = new Thread(InitializeServer);
            tr.IsBackground = true;
            tr.Start();
        }

        private void InitializeServer()
        {
            try
            {
                CenterServer.Init(Program.IMGFilename);

                Invoke((MethodInvoker)delegate
                {
                    Text += " (" + Program.IMGFilename + ")";
                    if (CenterServer.Tespia)
                    {
                        Text += " -TESPIA MODE-";
#if DEBUG
                        Text += " -DEBUG-";
#endif
                        Icon = Resources.Tespia;
                        ShowIcon = true;
                    }
                });

                MasterThread.RepeatingAction.Start(
                   "Server List Updator",
                   date =>
                   {
                       try
                       {
                           BeginInvoke((MethodInvoker)delegate
                           {
                               UpdateServerList();
                               txtPingEntries.Text = Pinger.CurrentLoggingConnections.ToString();
                           });
                       }
                       catch { }
                   },
                   0,
                   1000
               );

                // Update the online player count every minute
                MasterThread.RepeatingAction.Start(
                    "OnlinePlayerCounter",
                    date =>
                    {
                        int totalCount = 0;
                        foreach (var kvp in CenterServer.Instance.LocalServers)
                        {
                            var ls = kvp.Value;
                            if (ls.Connected == false) continue;
                            if (ls.Type != LocalServerType.Login &&
                                ls.Type != LocalServerType.Game &&
                                ls.Type != LocalServerType.Shop) continue;
                            totalCount += ls.Connections;
                            _onlinePlayerLog.Info(new OnlinePlayerCount
                            {
                                count = ls.Connections,
                                serverName = ls.Name
                            });
                        }

                        _onlinePlayerLog.Info(new OnlinePlayerCount
                        {
                            count = totalCount,
                            serverName = "TotalCount-" + CenterServer.Instance.World.ID
                        });
                    },
                    60000,
                    60000
                );

                Pinger.Init(Program.MainForm.LogAppend, Program.MainForm.LogAppend);
            }
            catch (Exception ex)
            {
                Program.LogFile.WriteLine("Got exception @ frmMain::InitServer : {0}", ex.ToString());
                MessageBox.Show($"[{DateTime.Now}][CENTER SERVER] Got exception @ frmMain::InitServer : {ex}");
                Environment.Exit(5);
            }
        }

        public void UpdateServerList()
        {
            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)UpdateServerList);
                return;
            }

            _totalConnections = 0;
            ListViewItem item;
            lvServers.BeginUpdate();
            byte loginCount = 0;
            foreach (var Server in CenterServer.Instance.LocalServers)
            {
                LocalServer ls = Server.Value;
                _totalConnections += ls.Connections;
                if (lvServers.Items.ContainsKey(Server.Key))
                {
                    item = lvServers.Items[Server.Key];
                }
                else
                {
                    item = new ListViewItem(new[] {
                        ls.Name,
                        ls.PublicIP + ":" + ls.Port,
                        "0",
                        "N/A"
                    });
                    item.Name = ls.Name;

                    lvServers.Items.Add(item);
                }

                item.ImageIndex = ls.Connected ? 1 : 0;
                item.SubItems[2].Text = ls.Connections.ToString();
                if (ls.IsGameServer)
                {
                    item.SubItems[0].Text = ls.Name + (ls.Connected ? " (CH. " + (ls.ChannelID + 1) + ")" : "");
                    item.SubItems[3].Text = $"{ls.RateMobEXP}/{ls.RateMesoAmount}/{ls.RateDropChance}";
                }

                if (ls.Connected)
                {
                    if (ls.Type == LocalServerType.Game)
                    {
                        RedisBackend.Instance.SetPlayerOnlineCount(
                            CenterServer.Instance.World.ID,
                            ls.ChannelID,
                            ls.Connections
                        );
                    }
                    else if (ls.Type == LocalServerType.Shop)
                    {
                        RedisBackend.Instance.SetPlayerOnlineCount(
                            CenterServer.Instance.World.ID,
                            50 + ls.ChannelID,
                            ls.Connections
                        );
                    }
                    else if (ls.Type == LocalServerType.Login)
                    {
                        RedisBackend.Instance.SetPlayerOnlineCount(
                            -1,
                            loginCount,
                            ls.Connections
                        );
                        loginCount++;
                    }
                }
            }
            lvServers.EndUpdate();

            txtTotalConnections.Text = _totalConnections.ToString();
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_totalConnections > 0)
            {
                if (MessageBox.Show("Are you sure you want to close the server? There's still people online!!!", "", MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }

            CenterServer.Instance.CharacterDatabase.Stop = true;
            MasterThread.Instance.Stop = true;
        }

        public void LogAppend(string what, params object[] args) => LogAppend(string.Format(what, args));
        public void LogAppend(string what)
        {
            BeginInvoke((MethodInvoker)delegate
           {
               txtLog.AddLine(what);
               CenterServer.Instance.LogToLogfile(what);
           });
        }

        public void LogDebug(string what)
        {
#if DEBUG
            LogAppend(what);
#endif
        }
    }
}