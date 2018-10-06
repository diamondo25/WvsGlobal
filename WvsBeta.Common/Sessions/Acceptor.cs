using System;
using System.Net;
using System.Net.Sockets;

namespace WvsBeta.Common.Sessions
{
    public abstract class Acceptor
    {
        public ushort Port { get; private set; }

        private TcpListener _listener;
        private TcpListener _listener6;
        
        protected Acceptor(ushort pPort)
        {
            Port = pPort;
            Start();
        }

        private bool Stopped = true;

        public void Start()
        {
            if (!Stopped) return;

            // IPv6 on Mono binds on IPv4 too.
            if (Type.GetType("Mono.Runtime") == null) {
                _listener = new TcpListener(IPAddress.Any, Port);
                _listener.Start(200);
            }
            _listener6 = new TcpListener(IPAddress.IPv6Any, Port);
            _listener6.Start(200);
            Stopped = false;
            _listener?.BeginAcceptSocket(EndAccept, _listener);
            _listener6.BeginAcceptSocket(EndAccept, _listener6);
        }

        public void Stop()
        {
            if (Stopped) return;
            Stopped = true;

            _listener?.Stop();
            _listener = null;

            _listener6?.Stop();
            _listener6 = null;
        }

        private void EndAccept(IAsyncResult pIAR)
        {
            if (Stopped) return;

            var listener = (TcpListener)pIAR.AsyncState;

            try
            {
                OnAccept(listener.EndAcceptSocket(pIAR));
            }
            catch { }

            if (Stopped) return;
            listener?.BeginAcceptSocket(EndAccept, listener);
        }

        public abstract void OnAccept(Socket pSocket);
    }
}
