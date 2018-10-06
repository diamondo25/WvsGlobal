using WvsBeta.Common.Sessions;

namespace WvsBeta.Game
{
    public static class AdminPacket
    {
        public static void HandleAdminCommandMessage(Character chr, Packet packet)
        {
            byte to = packet.ReadByte();
            byte TypeMessage = packet.ReadByte(); //   /alert, /notice, /slide
            string Message = packet.ReadString();
            switch (to)
            {
                case 0x00: //To every game server
                MessagePacket.SendAdminMessage(chr, Message, TypeMessage, 0);
                break;
                case 0x01: //To channel
                MessagePacket.SendAdminMessage(chr, Message, TypeMessage, 1);
                break;
                case 0x02: //To map
                MessagePacket.SendAdminMessage(chr, Message, TypeMessage, 2);
                break;
            }
        }

        public static void Hide(Character chr, bool hide)
        {
            Packet pw = new Packet(ServerMessages.ADMIN_RESULT);
            pw.WriteByte(15);
            pw.WriteBool(hide);
            chr.SendPacket(pw);
        }

        public static void BanCharacterMessage(Character chr)
        {
            Packet pw = new Packet(ServerMessages.ADMIN_RESULT);
            pw.WriteByte(4);
            pw.WriteByte(0); // Not used?
            chr.SendPacket(pw);
        }

        public static void InvalidNameMessage(Character chr)
        {
            Packet pw = new Packet(ServerMessages.ADMIN_RESULT);
            pw.WriteByte(6);
            pw.WriteByte(1); // Anything higher than 0
            chr.SendPacket(pw);
        }

        public static void UnrankSuccessful(Character chr)
        {
            // You have successfully removed the name from the ranks.
            Packet pw = new Packet(ServerMessages.ADMIN_RESULT);
            pw.WriteByte(6);
            pw.WriteByte(0); // Anything lower or equal to 0
            chr.SendPacket(pw);
        }

        public static void VarGetMessage(Character chr, string Name, string Var, string Value)
        {
            //format ; {string} : {string} = {string} 
            Packet pw = new Packet(ServerMessages.ADMIN_RESULT);
            pw.WriteByte(8);
            pw.WriteString(Name); // Empty name = You have either entered a wrong NPC name or
            pw.WriteString(Var);
            pw.WriteString(Value);
            chr.SendPacket(pw);
        }

        public static void RequestFailed(Character chr)
        {
            // Your request failed.
            Packet pw = new Packet(ServerMessages.ADMIN_RESULT);
            pw.WriteByte(18);
            pw.WriteByte(0); // not 0 adds something to the chatlog??
            chr.SendPacket(pw);
        }

        public static void SentWarning(Character chr, bool succeeded)
        {
            // Your request failed.
            Packet pw = new Packet(ServerMessages.ADMIN_RESULT);
            pw.WriteByte(19);
            pw.WriteBool(succeeded);
            chr.SendPacket(pw);
        }
    }
}
