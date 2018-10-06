using WvsBeta.Common.Sessions;

namespace WvsBeta.Shop
{
    class ShopAcceptor : Acceptor
    {
        public ShopAcceptor() : base(Server.Instance.Port)
        {
        }

        public override void OnAccept(System.Net.Sockets.Socket pSocket)
        {
            new ClientSocket(pSocket);
        }
    }
}
