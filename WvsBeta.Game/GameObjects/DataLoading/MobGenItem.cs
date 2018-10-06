using System;
using WvsBeta.Common;

namespace WvsBeta.Game
{
    public class MobGenItem
    {
        public int ID { get; }
        public int RegenInterval { get; set; }
        public long RegenAfter { get; set; }
        public short Foothold { get; }
        public bool FacesLeft { get; }
        public int MobCount { get; set; }

        private bool _initializedYAxis;
        private short _y, _cy;
        public short X { get; }

        public short Y
        {
            get
            {
                if (_initializedYAxis) return _y;

                MobData md;
                if (DataProvider.Mobs.TryGetValue(ID, out md) == false)
                {
                    Console.WriteLine($"Invalid mob template ID({ID})");
                    return -1;
                }

                // Flying mobs use CY value
                //if (md.Flies)
                    _y = _cy;
                _initializedYAxis = true;
                return _y;
            }
        }

        public MobGenItem(Life life, long? currentTime)
        {
            ID = life.ID;
            Foothold = (short)life.Foothold;
            FacesLeft = life.FacesLeft;
            X = life.X;
            _initializedYAxis = false;
            _y = life.Y;
            _cy = life.Cy;
            MobCount = 0;

            if (currentTime == null) currentTime = MasterThread.CurrentTime;

            var regenInterval = RegenInterval = life.RespawnTime * 1000;
            if (regenInterval >= 0)
            {
                var T1 = regenInterval / 10;
                var T2 = 6 * regenInterval / 10;

                RegenAfter = currentTime.Value;
                if (T2 != 0)
                    RegenAfter += T1 + Rand32.Next() % T2;
                //else
                //    RegenAfter += Rand32.Next();
            }
            else
            {
                RegenAfter = 0;
            }
        }


    }
}