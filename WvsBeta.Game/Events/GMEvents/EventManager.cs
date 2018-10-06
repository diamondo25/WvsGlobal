using System;
using MySql.Data.MySqlClient;
using WvsBeta.Common;
using System.Collections.Generic;
using System.Linq;

namespace WvsBeta.Game.Events.GMEvents
{
    public enum EventType
    {
        Jewel,
        Snowball,
        Fitness,
        Quiz,
        Tournament
    }

    public class EventManager
    {
        private static EventManager _instance = null;
        public static EventManager Instance
        {
            private set
            {
                _instance = value;
            }

            get
            {
                if (_instance == null)
                {
                    _instance = new EventManager();
                }
                return _instance;
            }
        }

        public readonly Dictionary<EventType, Event> EventInstances = new Dictionary<EventType, Event>()
        {
            {EventType.Jewel, new MapleJewelEvent() },
            {EventType.Snowball, new MapleSnowballEvent() },
            {EventType.Fitness, new MapleFitnessEvent() },
            {EventType.Quiz, new MapleQuizEvent() },
            {EventType.Tournament, null }
        };
    }

    public static class EventHelper
    {
        private static readonly string LastMapKey = "GMEvent-LastMap";

        public static void SetParticipated(int charid) //function to set time and date of event participation
        {
            Server.Instance.CharacterDatabase.RunQuery("UPDATE characters SET event = NOW() WHERE ID = @charid", "@charid", charid);
        }

        public static bool HasParticipated(string charname) //this checks to see if a user has participated in an event in the last 24 hours.
        {
            var data = Server.Instance.CharacterDatabase.RunQuery("SELECT `event` FROM characters WHERE name = @name", "@name", charname) as MySqlDataReader;

            var lastEvent = data.Map(r => r.GetDateTime("event"));

            if (MasterThread.CurrentDate <= lastEvent.AddDays(1))
                return true;
            return false;
        }

        //TODO refactor to use something more robust than charactervariables
        public static void SetLastMap(Character mongoloid, int mapId)
        {
            mongoloid.Variables.SetVariableData(LastMapKey, mapId.ToString());
        }

        public static int GetLastMap(Character mongoloid)
        {
            var id = mongoloid.Variables.GetVariableData(LastMapKey);
            if (int.TryParse(id, out int map))
            {
                return map;
            }
            return -1;
        }

        public static void ReturnLastMap(Character mongoloid)
        {
            int dest = GetLastMap(mongoloid);
            dest = dest == -1 ? 104000000 /*Lith Harbor*/ : dest;
            mongoloid.ChangeMap(dest);
        }

        public static void OpenAllPortals(IEnumerable<Map> maps)
        {
            maps.ForEach(m => m.PortalsOpen = true);
        }

        public static void CloseAllPortals(IEnumerable<Map> maps)
        {
            maps.ForEach(m => m.PortalsOpen = false);
        }

        public static void ApplyTimer(IEnumerable<Map> maps, int runtimeSeconds)
        {
            maps.ForEach(m => m.StartTimer(runtimeSeconds));
        }

        public static void ResetTimer(IEnumerable<Map> maps)
        {
            maps.ForEach(m => m.TimerEndTime = MasterThread.CurrentTime);
        }

        public static void WarpEveryone(IEnumerable<Map> maps, int destinationId)
        {
            maps
                .SelectMany(m => m.Characters)
                .ToList()
                .ForEach(c => c.ChangeMap(destinationId));
        }

        public static void WarpEveryone(Map map, int destinationId)
        {
            map.Characters.ToList().ForEach(c => c.ChangeMap(destinationId));
        }
    }
}
