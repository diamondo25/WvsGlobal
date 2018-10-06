using System;
namespace WvsBeta.Center
{
    public class WorldEvent
    {
        public string Name { get; set; }
        public string ScrollingHeader { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public double ExpRate { get; set; }
        public double DropRate { get; set; }
        public double MesoRate { get; set; }
    }
}
