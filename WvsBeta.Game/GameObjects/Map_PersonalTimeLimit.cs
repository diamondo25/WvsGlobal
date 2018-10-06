using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace WvsBeta.Game.GameObjects
{
    class Map_PersonalTimeLimit : Map
    {
        readonly Dictionary<int, long> _times = new Dictionary<int, long>();

        public Map_PersonalTimeLimit(int id) : base(id)
        {
            Trace.WriteLine($"Found PersonalTimeLimit map {id}");
        }

        public override void AddPlayer(Character chr)
        {
            base.AddPlayer(chr);

            if (!chr.IsGM)
            {
                _times[chr.ID] = MasterThread.CurrentTime + (TimeLimit * 1000);
                MapPacket.ShowMapTimerForCharacter(chr, TimeLimit);
            }
        }

        public override void RemovePlayer(Character chr, bool gmhide = false)
        {
            base.RemovePlayer(chr, gmhide);

            _times.Remove(chr.ID);
        }

        public override void MapTimer(long pNow)
        {
            base.MapTimer(pNow);

            foreach (var keyValuePair in _times.Where(x => x.Value < pNow).ToArray())
            {
                GetPlayer(keyValuePair.Key)?.ChangeMap(ReturnMap);
            }
        }
    }
}
