using System;
using System.Diagnostics;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;
using WvsBeta.Game.Events.GMEvents;
using WvsBeta.Game.GameObjects;
using WvsBeta.Game.Properties;

namespace WvsBeta.Game
{
    class GameMainForm : MainFormConsole
    {
        private int load = 0;

        public override void LogToFile(string what)
        {
            Program.LogFile.WriteLine(what);
        }

        public override void Shutdown()
        {
            Shutdown(null);
        }

        public override void InitializeServer()
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

            Server.Instance.LoadFieldSet();
            MasterThread.RepeatingAction.Start("Map Checker", Server.Instance.CheckMaps, 0, 1000);
            MasterThread.RepeatingAction.Start("FieldSet Checker", FieldSet.Update, 0, 1000);


            MasterThread.RepeatingAction.Start(
                "Console title updater",
                x =>
                {
                    var mt = MasterThread.Instance;
                    Console.Title = $"Game CH.{(Server.Instance.ID + 1)} ({Program.IMGFilename}) {load} Stats {mt.CurrentCallbackQueueLength}/{mt.RegisteredRepeatingActions}/{Pinger.CurrentLoggingConnections}";
                    if (Server.Tespia)
                    {
                        Console.Title += " -TESPIA MODE-";
                    }
                },
                0,
                2000
            );

            MasterThread.RepeatingAction.Start("Log killed mobs",
                curTime =>
                {
                    DataProvider.Maps.Values.ForEach(x => x.FlushMobKillCount());
                },
                0,
                60 * 1000
            );


            LogAppend("Server successfully booted!");

            if (Server.Instance.InMigration)
            {
                // Tell the other server to start migrating...
                var pw = new Packet(ISClientMessages.ServerMigrationUpdate);
                pw.WriteByte((byte)ServerMigrationStatus.StartMigration);
                Server.Instance.CenterConnection.SendPacket(pw);
            }

            if (Server.Tespia && !IsMono)
            {
                var curProcess = GetConsoleWindow();
                SendMessage(curProcess, WM_SETICON, ICON_BIG, Resources.Tespia.Handle);
                SendMessage(curProcess, WM_SETICON, ICON_SMALL, Resources.Tespia.Handle);
                curProcess = Process.GetCurrentProcess().Handle;
                SendMessage(curProcess, WM_SETICON, ICON_BIG, Resources.Tespia.Handle);
                SendMessage(curProcess, WM_SETICON, ICON_SMALL, Resources.Tespia.Handle);
            }
            Pinger.Init(x => Program.MainForm.LogAppend(x), x => Program.MainForm.LogAppend(x));
        }


        public override void ChangeLoad(bool up)
        {
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

            Server.Instance.CenterConnection.SendUpdateConnections(load);
        }


        private static bool alreadyShuttingDown = false;
        private static bool forceShutDown = false;

        public override void Shutdown(ConsoleCancelEventArgs args)
        {
            if (forceShutDown) return;

            if (args != null) args.Cancel = true;
            StartShutdown();
        }

        private void StartShutdown()
        {
            if (alreadyShuttingDown)
            {
                return;
            }
            alreadyShuttingDown = true;

            Program.MainForm.LogAppend("Getting rid of players");

            int timeoutSeconds = 10;

            var startTime = MasterThread.CurrentTime;
            MasterThread.RepeatingAction ra = null; // Prevent error
            ra = new MasterThread.RepeatingAction(
                "Client DC Thread",
                (date) =>
                {
                    var isTimeout = (date - startTime) > timeoutSeconds * 1000;
                    if (Server.Instance.PlayerList.Count == 0 || isTimeout)
                    {
                        var queueLen = MasterThread.Instance.CurrentCallbackQueueLength;
                        var waiting = (long)Math.Min(60000, Math.Max(1000, queueLen * 500));
                        LogAppend($"Preparing shutdown... Timeout? {isTimeout} Queue size: {queueLen}, connections {Pinger.CurrentLoggingConnections}, waiting for {waiting} millis");

                        MasterThread.RepeatingAction.Start(
                            "Server finalizing and shutdown thread",
                            (date2) =>
                            {
                                forceShutDown = true;
                                Server.Instance.CenterConnection?.Disconnect();
                                MasterThread.Instance.Stop = true;
                                Environment.Exit(0);
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
        }

        public override void HandleCommand(string name, string[] args)
        {
        }
    }
}
