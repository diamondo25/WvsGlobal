using System;

namespace WvsBeta.Common
{
    public class Tools
    {
        public static long GetFileTimeWithAddition(TimeSpan span)
        {
            return (MasterThread.CurrentDate + span).ToFileTimeUtc();
        }


        public static long GetTimeAsMilliseconds(DateTime pNow)
        {
            return pNow.ToFileTime() / 10000;
        }
    }
}
