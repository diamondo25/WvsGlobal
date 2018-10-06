using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WvsBeta.Game.Events.GMEvents
{
    public abstract class Event
    {
        public bool JoinEnabled
        {
            get;
            private set;
        }

        public bool InProgress
        {
            get;
            private set;
        }

        public Event()
        {
            JoinEnabled = false;
            InProgress = false;
        }

        public virtual void Prepare()
        {
            JoinEnabled = true;
            InProgress = false;
        }

        public virtual void Join(Character chr)
        {
            EventHelper.SetLastMap(chr, chr.MapID);
            EventHelper.SetParticipated(chr.ID);
        }

        public virtual void Start(bool joinDuringEvent = false)
        {
            InProgress = true;
            JoinEnabled = joinDuringEvent;
        }

        public virtual void Stop()
        {
            JoinEnabled = false;
            InProgress = false;
        }
    }
}
