using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WvsBeta.Common.Sessions;

namespace WvsBeta.Common.Tracking
{ 
    public class PacketLog
    {
        private static ILog log = LogManager.GetLogger("HackLog");

        public int header { get; set; }
        public string header_byte_name { get; set; }
        public string packetBytes { get; set; }
        public int length { get; set; }
        public string logID = "Packet Log";
        public string server { get; set; }
        public string clientIp { get; set; }


        public static void ReceivedPacket(Packet packet, byte head, string pServer, string pClientIp)
        {
            log.Info(new PacketLog()
            {
                packetBytes = packet.ToString(),
                length = packet.Length,
                header = head,
                header_byte_name = Enum.GetName(typeof(ClientMessages), (ClientMessages)head),
                server = pServer,
                clientIp = pClientIp
            });
        }
    }
}
