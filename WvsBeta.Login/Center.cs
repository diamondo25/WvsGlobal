using System.Net;

namespace WvsBeta.Login
{
    public class Center
    {
        public ushort Port { get; set; }
        public IPAddress IP { get; set; }
        public byte ID { get; set; }
        public byte Channels { get; set; }
        public byte State { get; set; }
        public string EventDescription { get; set; }
        public bool BlockCharacterCreation { get; set; }
        public bool AdultWorld { get; set; }
        public string Name { get; set; }

        public int[] UserNo { get; set; }

        public CenterSocket Connection { get; private set; }

        public bool IsConnected => Connection != null && !Connection.Disconnected;

        public void Connect()
        {
            Connection = new CenterSocket(IP.ToString(), Port, this);
        }
    }

}
