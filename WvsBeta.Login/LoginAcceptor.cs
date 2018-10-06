using WvsBeta.Common.Sessions;

namespace WvsBeta.Login
{
    class LoginAcceptor : Acceptor
    {
        public LoginAcceptor() : base(Server.Instance.Port)
        {

        }

        public override void OnAccept(System.Net.Sockets.Socket pSocket)
        {
            new ClientSocket(pSocket);
        }
    }
}