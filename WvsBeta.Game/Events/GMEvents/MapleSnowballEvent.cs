using System.Collections.Generic;
using WvsBeta.Common;
using WvsBeta.Game.Events.GMEvents;
using System.Linq;
using WvsBeta.Game.GameObjects;
using static WvsBeta.MasterThread;
using static WvsBeta.Game.GameObjects.Map_Snowball;
using System;
using log4net;
using log4net.Core;

namespace WvsBeta.Game.Events
{
    class MapleSnowballEvent : Event
    {
        private static ILog _log = LogManager.GetLogger("MapleSnowballEvent");
        private static readonly int sMapId = 109060000;
        private static readonly Map_Snowball SnowballMap = (Map_Snowball)DataProvider.Maps[sMapId];
        private static readonly int lobbyMapId = 109060001;
        private static readonly Map LobbyMap = DataProvider.Maps[lobbyMapId];
        private static readonly int WinMapId = 109050000;
        private static readonly int LoseMapId = 109050001;

        public static int EventTimeSeconds = 60 * 10; //10 Minutes

        private readonly HashSet<Character> MapleTeam;
        private readonly HashSet<Character> StoryTeam;
        private RepeatingAction End;

        public MapleSnowballEvent() : base()
        {
            MapleTeam = new HashSet<Character>();
            StoryTeam = new HashSet<Character>();
        }

        public override void Prepare()
        {
            MapleTeam.Clear();
            StoryTeam.Clear();
            SnowballMap.Reset();
            base.Prepare();
        }

        public override void Join(Character chr)
        {
            base.Join(chr);
            chr.ChangeMap(lobbyMapId);
        }

        public override void Start(bool joinDuringEvent = false)
        {
            foreach (var chr in LobbyMap.Characters.ToList())
            {
                if (MapleTeam.Count < StoryTeam.Count)
                {
                    MapleTeam.Add(chr);
                    chr.ChangeMap(sMapId, SnowballMap.Top.Name);
                }
                else
                {
                    StoryTeam.Add(chr);
                    chr.ChangeMap(sMapId, SnowballMap.Bottom.Name);
                }
            }

            Program.MainForm.LogDebug("Starting..." + " Maple " + string.Join(", ", MapleTeam.Select(c => c.Name)) + "... Story " + string.Join(", ", StoryTeam.Select(c => c.Name)));

            End = RepeatingAction.Start("SnowballWatcher", Stop, EventTimeSeconds * 1000, 0);
            SnowballMap.StartTimer(EventTimeSeconds);
            SnowballMap.SnowballState = SnowballEventState.IN_PROGRESS;
            base.Start(joinDuringEvent);
        }

        public override void Stop()
        {
            Program.MainForm.LogDebug("Stopping." + Environment.StackTrace);
            End?.Stop();
            End = null;

            List<Character> Winners;
            List<Character> Losers;

            if (SnowballMap.GetWinner() == SnowballEventState.MAPLE_WIN)
            {
                Winners = MapleTeam.ToList();
                Losers = StoryTeam.ToList();
            }
            else
            {
                Winners = StoryTeam.ToList();
                Losers = MapleTeam.ToList();
            }

            _log.Info("Total players: " + (Winners.Count + Losers.Count));
            _log.Info("Winners: " + string.Join(", ", Winners.Select(x => x.Name)));
            _log.Info("Losers: " + string.Join(", ", Losers.Select(x => x.Name)));

            Winners.ForEach(c =>
            {
                MapPacket.MapEffect(c, 4, "Coconut/Victory", true);
                MapPacket.MapEffect(c, 3, "event/coconut/victory", true);
            });
            Losers.ForEach(c =>
            {
                MapPacket.MapEffect(c, 4, "Coconut/Failed", true);
                MapPacket.MapEffect(c, 3, "event/coconut/lose", true);
            });

            RepeatingAction.Start("snowball warper", e =>
            {
                Winners.ForEach(c => c.ChangeMap(WinMapId));
                Losers.ForEach(c => c.ChangeMap(LoseMapId));
                SnowballMap.TimerEndTime = MasterThread.CurrentTime;
                MapleTeam.Clear();
                StoryTeam.Clear();
                SnowballMap.Reset();
                base.Stop();
            }, 10 * 1000, 0);
        }

        public void PlayerLeft(Character chr)
        {
            MapleTeam.Remove(chr);
            StoryTeam.Remove(chr);
        }
    }
}
