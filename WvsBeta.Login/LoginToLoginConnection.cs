using System.Net.Sockets;
using System.Windows.Forms;
using WvsBeta.Common.Sessions;

namespace WvsBeta.Login
{
    class LoginToLoginConnection : AbstractConnection
    {
        public LoginToLoginConnection(Socket pSocket) : base(pSocket)
        {
            SendHandshake(9994, "CenterToCenter", 99);
        }

        public LoginToLoginConnection(string pIP, ushort pPort) : base(pIP, pPort)
        {
        }

        public override void AC_OnPacketInbound(Packet pPacket)
        {
            switch ((ISServerMessages)pPacket.ReadByte())
            {
                case ISServerMessages.ServerMigrationUpdate:


                    switch ((ServerMigrationStatus)pPacket.ReadByte())
                    {
                        case ServerMigrationStatus.StartMigration:
                            {
                                Program.MainForm.LogAppend("Starting migration");
                                Server.Instance.InMigration = true;
                                Server.Instance.StopListening();
                                var pw = new Packet(ISServerMessages.ServerMigrationUpdate);
                                pw.WriteByte((byte)ServerMigrationStatus.StartListening);
                                SendPacket(pw);
                                break;
                            }
                        case ServerMigrationStatus.StartListening:
                            {
                                Server.Instance.StartListening();
                                Program.MainForm.LogAppend("Starting listening, requesting data");
                                var pw = new Packet(ISServerMessages.ServerMigrationUpdate);
                                pw.WriteByte((byte)ServerMigrationStatus.DataTransferRequest);
                                SendPacket(pw);
                                break;
                            }
                        case ServerMigrationStatus.DataTransferRequest:
                            {
                                Program.MainForm.LogAppend("Sending data");
                                SendCurrentConfiguration();
                                break;
                            }
                        case ServerMigrationStatus.DataTransferResponse:
                            {
                                Program.MainForm.LogAppend("Receiving data...");

                                Program.MainForm.LogAppend("Request server migration");
                                var pw = new Packet(ISServerMessages.ServerMigrationUpdate);
                                pw.WriteByte((byte)ServerMigrationStatus.FinishedInitialization);
                                SendPacket(pw);

                                Server.Instance.StartLTLAcceptor();

                                break;
                            }

                        case ServerMigrationStatus.FinishedInitialization:
                            {
                                Program.MainForm.LogAppend("Other login server finished.");

                                this.Disconnect();
                                Application.Exit();
                                break;
                            }
                    }

                    break;
            }
        }

        public void SendCurrentConfiguration()
        {
            var pw = new Packet(ISServerMessages.ServerMigrationUpdate);
            pw.WriteByte((byte)ServerMigrationStatus.DataTransferResponse);

            // Nothing to transfer

            SendPacket(pw);
        }
    }
}
