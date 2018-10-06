using WvsBeta.Common.Sessions;

namespace WvsBeta.Game.Handlers
{
    class BuddyHandler
    {
        public static void HandleBuddy(Character chr, Packet packet)
        {
            byte header = packet.ReadByte(); //Which case
            switch (header)
            {
                case 0: // Update
                {
                    Server.Instance.CenterConnection.BuddyUpdate(chr);
                    break;
                }
                case 1: //Invite
                {
                    string Victim = packet.ReadString();
                    Server.Instance.CenterConnection.BuddyRequest(chr, Victim);
                    break;
                }
                case 2: //Accept
                {
                    Server.Instance.CenterConnection.BuddyAccept(chr);
                    break;
                }
                case 3: // Decline and Delete
                {
                    int Victim = packet.ReadInt();
                    Server.Instance.CenterConnection.BuddyDecline(chr, Victim);
                    break;
                }
                default:
                {
                    Program.MainForm.LogAppend("wat buddy op is diz: " + header);
                    break;
                }
            }

        }
    }
}
