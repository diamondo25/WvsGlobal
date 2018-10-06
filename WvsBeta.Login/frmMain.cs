using System;
using System.Linq;
using System.Windows.Forms;
using WvsBeta.Common;
using WvsBeta.Login.Properties;

namespace WvsBeta.Login
{
    public partial class frmMain : Form
    {
        int load = 0;

        public frmMain()
        {
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            txtLoad.Text = "0";
            txtPingEntries.Text = "0";
            var tr = new System.Threading.Thread(InitializeServer)
            {
                IsBackground = true
            };
            tr.Start();
        }

        private void InitializeServer()
        {
            try
            {
                Server.Init(Program.IMGFilename);


                this.Invoke((MethodInvoker)delegate
                {
                    this.Text += " (" + Program.IMGFilename + ")";
                    txtLog.AddLine("Loaded as " + Program.IMGFilename);
                    if (Server.Tespia)
                    {
                        Text += " -TESPIA MODE-";
                        Icon = Resources.Tespia;
                        ShowIcon = true;
                    }
                });


                Pinger.Init(Program.MainForm.LogAppend, Program.MainForm.LogAppend);

                MasterThread.RepeatingAction.Start(
                    "Form Updater",
                    (date) =>
                    {
                        try
                        {
                            this.BeginInvoke((MethodInvoker)delegate
                           {
                               txtPingEntries.Text = Pinger.CurrentLoggingConnections.ToString();
                               txtLoad.Text = load.ToString();
                           });
                        }
                        catch { }
                    },
                    0,
                    1500
                );

            }
            catch (Exception ex)
            {
                Program.LogFile.WriteLine("Got exception @ frmMain::InitializeServer : {0}", ex.ToString());
                MessageBox.Show($"[LOGIN SERVER] Got exception @ frmMain::InitializeServer : {ex.ToString()}");
                Environment.Exit(5);
            }
        }

        public void ChangeLoad(bool up)
        {
            if (up)
            {
                ++load;
                txtLog.AddLine($"Client Connected. Current User count: {load}");
            }
            else
            {
                --load;
                txtLog.AddLine($"Client Disconnected. Current User count: {load}");
            }

            foreach (var kvp in Server.Instance.Worlds)
            {
                if (!kvp.Value.IsConnected) continue;
                kvp.Value.Connection.UpdateConnections(load);
            }
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Server.Instance.UsersDatabase != null)
                Server.Instance.UsersDatabase.Stop = true;
            MasterThread.Instance.Stop = true;
        }

        public void LogAppend(string what, params object[] args) => LogAppend(string.Format(what, args));
        public void LogAppend(string what)
        {
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
    }
}
