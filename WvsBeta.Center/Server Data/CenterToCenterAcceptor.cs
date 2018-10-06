using System.Net.Sockets;
using WvsBeta.Common.Sessions;

namespace WvsBeta.Center
{
    public class CenterToCenterAcceptor : Acceptor
    {
        public CenterToCenterAcceptor(ushort port) : base(port)
        {
        }

        public override void OnAccept(Socket pSocket)
        {
            new CenterToCenterConnection(pSocket);
        }
    }
}
