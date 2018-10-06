using log4net;

namespace WvsBeta.Common.Tracking
{
    public class ClientChecksum
    {
        private static ILog log = LogManager.GetLogger("HackLog");

        public string machineID { get; set; }
        public string clientIp { get; set; }
        public string filename { get; set; }
        public string checksum { get; set; }

        public static void LogFileChecksum(string mid, string ip, string fname, string chksum)
        {
            log.Info(new ClientChecksum()
            {
                machineID = mid,
                clientIp = ip,
                filename = fname,
                checksum = chksum
            });
        }

        public static void NoChecksum(string mid, string ip)
        {
            log.Info(new ClientChecksum()
            {
                machineID = mid,
                clientIp = ip,
                filename = "N/A",
                checksum = "Player did not send checksum"
            });
        }
    }
}
