using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WvsBeta.Common;

namespace WvsBeta.Game
{
    public class FieldSet
    {
        public static Dictionary<string, FieldSet> Instances { get; } = new Dictionary<string, FieldSet>();
        public string Name { get; private set; }
        public Map[] Maps { get; private set; }
        public int Timeout { get; private set; }

        public bool Started { get; private set; }
        public long EndTime { get; private set; }
        public long TimeRemaining => EndTime - MasterThread.CurrentTime;

        public bool KickPeopleToReturnMap { get; private set; } = true;

        public FieldSet(ConfigReader.Node fsNode)
        {
            Name = fsNode.Name;
            Instances[Name] = this;

            var maps = new Map[0];

            foreach (var subNode in fsNode.SubNodes)
            {
                if (int.TryParse(subNode.Name, out var idx))
                {
                    if (idx >= maps.Length)
                        Array.Resize(ref maps, idx + 1);
                    var map = DataProvider.Maps[subNode.GetInt()];
                    maps[idx] = map;
                    map.ParentFieldSet = this;
                }
                else switch (subNode.Name)
                    {
                        case "timeOut":
                            Timeout = subNode.GetInt();
                            break;
                        case "kickPeopleToReturnMap":
                            KickPeopleToReturnMap = subNode.GetBool();
                            break;
                    }
            }

            Maps = maps;
            Program.MainForm.LogAppend("Loaded fieldset '{0}' with maps {1}", Name, string.Join(", ", Maps.Select(x => x.ID)));
        }

        public void Start()
        {
            Started = true;
            EndTime = MasterThread.CurrentTime + (Timeout * 1000);

            foreach (var map in Maps)
            {
                map.Reset(false); // bool Should be option from config
                map.OnEnter = RunTimer;
            }
            Program.MainForm.LogAppend("Started fieldset '{0}'", Name);
        }

        public void End()
        {
            foreach (var map in Maps)
            {
                map.OnEnter = null;
                map.OnTimerEnd?.Invoke(map);
                if (KickPeopleToReturnMap)
                {
                    foreach (var character in map.Characters.ToList())
                    {
                        character.ChangeMap(map.ForcedReturn);
                    }
                }
            }
            Started = false;
            Program.MainForm.LogAppend("Ended fieldset '{0}'", Name);
        }

        public static bool Enter(string name, Character chr, int mapIdx)
        {
            if (!Instances.TryGetValue(name, out var fs)) return false;
            // Todo: accept more people ?
            if (fs.Started) return false;
            fs.Start();
            var mapId = fs.Maps[mapIdx].ID;
            chr.ChangeMap(mapId);
            return true;
        }

        public void TryEndingIt(long currentTime)
        {
            if (!Started) return;

            var timesUp = EndTime < currentTime;
            var noMorePlayers = Maps.Sum(x => x.Characters.Count) == 0;

            if (timesUp || noMorePlayers)
            {
                End();
            }
        }

        public static void Update(long currentTime)
        {
            foreach (var keyValuePair in Instances)
            {
                keyValuePair.Value.TryEndingIt(currentTime);
            }
        }

        private void RunTimer(Character chr, Map map)
        {
            if (Started)
                MapPacket.ShowMapTimerForCharacter(chr, (int)(TimeRemaining / 1000));
        }
    }
}
