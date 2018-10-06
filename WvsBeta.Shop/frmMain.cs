using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using log4net.Core;
using WvsBeta.Common;
using WvsBeta.Shop.Properties;

namespace WvsBeta.Shop
{
    public partial class frmMain : Form
    {
        int load;
        public frmMain()
        {
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            txtLoad.Text = "0";
            Thread tr = new Thread(InitializeServer);
            tr.IsBackground = true;
            tr.Start();
        }

        private void InitializeServer()
        {
            Server.Init(Program.IMGFilename);

            LogAppend("Loading data file... ", false);
            DataProvider.Load();
            LogAppend("DONE");
            

            Invoke((MethodInvoker) delegate
            {
                Text += " (" + Program.IMGFilename + ")";
                if (Server.Tespia)
                {
                    Text += " -TESPIA MODE-";
                    Icon = Resources.Tespia;
                    ShowIcon = true;
                }
            });
        }

        public void SetLoad()
        {
            txtLoad.BeginInvoke((MethodInvoker)delegate
            {
                txtLoad.Text = load.ToString();
            });
        }

        public void ChangeLoad(bool up)
        {
            if (up)
                ++load;
            else
                --load;

            SetLoad();
            Server.Instance.CenterConnection.updateConnections(load);
        }

        public void LogAppend(string what, params object[] args)
        {
            what = string.Format(what, args);
            var keys = log4net.ThreadContext.Properties.GetKeys() ?? new string[0] { };
            var copy = keys.Select(x => new Tuple<string, object>(x, log4net.ThreadContext.Properties[x])).ToArray();

            MasterThread.Instance.AddCallback((date) =>
            {
                copy.ForEach(x => log4net.ThreadContext.Properties[x.Item1] = x.Item2);
                txtLog.AddLine(what);

                Server.Instance.LogToLogfile(what);

                copy.ForEach(x => log4net.ThreadContext.Properties.Remove(x.Item1));

            }, "LogAppend");
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
                        var waiting = (long)Math.Max(700, Math.Min(3000, queueLen * 100));
                        LogAppend($"Preparing shutdown... Timeout? {isTimeout} Queue size: {queueLen}, waiting for {waiting} millis");

                        MasterThread.Instance.AddRepeatingAction(new MasterThread.RepeatingAction(
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
                        ));
                        MasterThread.Instance.RemoveRepeatingAction(ra);
                    }
                    else
                    {
                        Server.Instance.PlayerList.ForEach(x =>
                        {
                            if (x.Value.Character == null) return;
                            try
                            {
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
    }
}
