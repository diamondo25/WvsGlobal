using System.Net.Sockets;
using WvsBeta.Common.Sessions;

namespace WvsBeta.Login
{
    class LoginToLoginAcceptor : Acceptor
    {
        public LoginToLoginAcceptor(ushort pPort) : base(pPort)
        {
        }

        public override void OnAccept(Socket pSocket)
        {
            Server.Instance.LoginToLoginConnection = new LoginToLoginConnection(pSocket);
            this.Stop();
        }
    }
}
