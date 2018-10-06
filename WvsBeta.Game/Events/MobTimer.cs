using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace WvsBeta.Game
{
    class MobTimer
    {

        public static Stopwatch stop = new Stopwatch();

        public static void Start()
        {
            stop.Start();
        }
    }
}
