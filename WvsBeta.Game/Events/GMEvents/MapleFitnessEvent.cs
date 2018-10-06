using System.Collections.Generic;
using System.Linq;
using static WvsBeta.MasterThread;

namespace WvsBeta.Game.Events.GMEvents
{
    class MapleFitnessEvent : Event
    {
        private static readonly int LobbyMapId = 109040000;
        private static readonly Map LobbyMap = DataProvider.Maps[LobbyMapId];
        private static readonly List<Map> Maps = new List<int>()
        {
            LobbyMapId,
            109040001,  //s1
            109040002,  //s2
            109040003,  //s3
            109040004,  //s4
        }.Select(id => DataProvider.Maps[id]).ToList();
        //portal in stage 4 automatically takes all winners to the win map
        private static readonly int LoseMapId = 109050001;
        private static int EventTimeLimitSeconds = 15 * 60; //15 minutes

        private RepeatingAction End = null;

        public override void Prepare()
        {
            EventHelper.CloseAllPortals(Maps);
            base.Prepare();
        }

        public override void Join(Character chr)
        {
            base.Join(chr);
            chr.ChangeMap(LobbyMapId, LobbyMap.SpawnPoints[0]);
        }

        public override void Start(bool joinDuringEvent = false)
        {
            LobbyMap.Characters.ToList().ForEach(c => c.ChangeMap(LobbyMapId, LobbyMap.SpawnPoints[0]));
            EventHelper.OpenAllPortals(Maps);
            EventHelper.ApplyTimer(Maps, EventTimeLimitSeconds);
            End = RepeatingAction.Start("FitnessWatcher", Stop, EventTimeLimitSeconds * 1000, 0);
            base.Start(joinDuringEvent);
        }

        public override void Stop()
        {
            End?.Stop();
            End = null;
            EventHelper.WarpEveryone(Maps, LoseMapId);
            EventHelper.ResetTimer(Maps);
            base.Stop();
        }
    }
}
