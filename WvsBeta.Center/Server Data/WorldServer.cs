using System;
using System.Collections.Generic;
using System.Linq;
using WvsBeta.Common;
using WvsBeta.Common.Sessions;

namespace WvsBeta.Center
{
    public class WorldServer
    {
        public byte ID { get; set; }
        public string Name { get; set; }
        public byte Channels { get; set; }
        public int UserWarning { get; set; }
        public int UserLimit { get; set; }

        public Dictionary<byte, LocalServer> GameServers { get; } = new Dictionary<byte, LocalServer>();
        public Dictionary<byte, LocalServer> ShopServers { get; } = new Dictionary<byte, LocalServer>();

        public List<WorldEvent> Events { get; } = new List<WorldEvent>();
        public WorldEvent RunningEvent { get; private set; } = null;
        
        public WorldServer(byte id)
        {
            ID = id;
        }

        public IEnumerable<LocalServer> GetOnlineGameServers() => GameServers.Select(x => x.Value).Where(x => x.Connected);
        public IEnumerable<LocalServer> GetOnlineShopServers() => ShopServers.Select(x => x.Value).Where(x => x.Connected);

        public void SendPacketToEveryGameserver(Packet packet)
        {
            GetOnlineGameServers().ForEach(x => x.ActiveServerConnection?.SendPacket(packet));
        }
        public void SendPacketToEveryShopserver(Packet packet)
        {
            GetOnlineShopServers().ForEach(x => x.ActiveServerConnection?.SendPacket(packet));
        }

        public byte GetFreeGameServerSlot()
        {
            foreach (var keyValuePair in GameServers)
            {
                if (keyValuePair.Value.Connected == false) return keyValuePair.Key;
            }

            return 0xff;
        }

        public byte GetFreeShopServerSlot()
        {
            foreach (var keyValuePair in ShopServers)
            {
                if (keyValuePair.Value.Connected == false) return keyValuePair.Key;
            }

            return 0xff;
        }

        public int CalculateWorldLoad()
        {
            return GameServers.Sum(x => x.Value.Connections) + ShopServers.Sum(x => x.Value.Connections);
        }

        public void AddWarning(Packet pw)
        {
            int load = CalculateWorldLoad();

            if (load > UserLimit) pw.WriteByte(2); // World is full
            else if (load > UserWarning) pw.WriteByte(1); // World is quite loaded, expect issues
            else pw.WriteByte(0);
        }

        public void CheckForEvents()
        {
            var currentDate = MasterThread.CurrentDate;

            if (RunningEvent != null)
            {
                // Not yet expired
                if (RunningEvent.EndTime >= currentDate) return;

                StopEvent();
            }
            
            foreach (var @event in Events)
            {
                if (@event.StartTime <= currentDate && @event.EndTime >= currentDate)
                {
                    Program.MainForm.LogAppend("Starting event '{0}'", @event.Name);

                    GameServers.ForEach(x =>
                    {
                        x.Value.SetRates(@event.ExpRate, @event.DropRate, @event.MesoRate, true);

                        if (!string.IsNullOrEmpty(@event.ScrollingHeader))
                        {
                            var p = new Packet(ISServerMessages.WSE_ChangeScrollingHeader);
                            p.WriteString(@event.ScrollingHeader);
                            x.Value.ActiveServerConnection?.SendPacket(p);
                        }
                    });
                    RunningEvent = @event;

                    break;
                }
            }
            
        }

        void StopEvent()
        {
            if (RunningEvent == null) return;
            var eventHadHeader = RunningEvent.ScrollingHeader != "";
            Program.MainForm.LogAppend("Stopping event...");
            RunningEvent = null;
            GameServers.ForEach(x =>
            {
                x.Value.ResetRates();
                if (eventHadHeader)
                {
                    var p = new Packet(ISServerMessages.WSE_ChangeScrollingHeader);
                    p.WriteString("");
                    x.Value.ActiveServerConnection?.SendPacket(p);
                }
            });
        }

        public void LoadEvents(ConfigReader reader)
        {
            StopEvent();
            Events.Clear();
            foreach (var eventNode in reader["events"])
            {
                var e = new WorldEvent();
                e.DropRate = eventNode["DROP_Rate"]?.GetDouble() ?? 1.0;
                e.ExpRate = eventNode["EXP_Rate"]?.GetDouble() ?? 1.0;
                e.MesoRate = eventNode["MESO_Rate"]?.GetDouble() ?? 1.0;
                e.StartTime = DateTime.Parse(eventNode["start"].Value).ToUniversalTime();
                e.EndTime = DateTime.Parse(eventNode["end"].Value).ToUniversalTime();
                e.Name = eventNode["name"].Value;
                e.ScrollingHeader = eventNode["header"]?.Value;

                // Ignore expired events
                if (e.EndTime < MasterThread.CurrentDate) continue;
                
                Program.MainForm.LogAppend("Loaded event {0}, running between {1} and {2} (UTC), rates {3}/{4}/{5}", e.Name, e.StartTime, e.EndTime, e.ExpRate, e.MesoRate, e.DropRate);
                Events.Add(e);
            }

            CheckForEvents();
        }
    }
}