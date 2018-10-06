using WvsBeta.Common.Sessions;

namespace WvsBeta.Game
{
    enum PartyRecvops : byte
    {
        LOAD = 0x00, //wat
        CREATE_NEW = 0x01,
        WITHDRAW = 0x02,
        JOIN = 0x03,
        INVITE = 0x04,
        EXPEL = 0x05
    }

    class PartyHandler
    {
        public static void HandleParty(Character chr, Packet pw)
        {
            PartyRecvops mode = (PartyRecvops)pw.ReadByte();

            switch (mode)
            {
                case PartyRecvops.CREATE_NEW: // CREATE PARTY
                    Server.Instance.CenterConnection.CreateParty(chr.ID);
                    break;
                case PartyRecvops.WITHDRAW: // LEAVE PARTY
                    Server.Instance.CenterConnection.LeaveParty(chr.ID);
                    break;
                case PartyRecvops.JOIN: // ACCEPT PARTY
                    Server.Instance.CenterConnection.AcceptParty(chr.ID);
                    break;
                case PartyRecvops.INVITE: // INVITE PARTY
                    int cid = pw.ReadInt();
                    Server.Instance.CenterConnection.InviteToParty(chr.ID, cid);
                    break;
                case PartyRecvops.EXPEL: // EXPEL PARTY
                    int fucker = pw.ReadInt();
                    Server.Instance.CenterConnection.ExpelFromParty(chr.ID, fucker);
                    break;
                default:
                    Program.MainForm.LogAppend("Unhandled Party OP: " + mode);
                    Program.MainForm.LogAppend("PACKET: " + pw.ToString());
                    break;
            }
        }

        public static void HandleDecline(Character chr, Packet packet)
        {
            Server.Instance.CenterConnection.DeclineParty(chr.ID, chr.Name);
        }
    }
}
