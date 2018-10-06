using WvsBeta.Common.Sessions;
using WvsBeta.Common.Tracking;

namespace WvsBeta.Game
{
    public static class MiscPacket
    {
        public static void ShowItemEffect(Character chr)
        {
            // Does not exist in client..?
            /*
            Packet pw = new Packet(ServerMessages.SHOW_ITEM_EFFECT);
            pw.WriteInt(chr.ID);
            pw.WriteInt(4090000); // Congrats song
            DataProvider.Maps[chr.Map].SendPacket(pw);
            */
        }
        public static void SendGotMesosFromLucksack(Character chr, int amount)
        {
            Packet pw = new Packet(ServerMessages.MESOBAG_SUCCEED);
            pw.WriteInt(amount);
            chr.SendPacket(pw);
        }

        public static void SendMesoFromLucksackFailed(Character chr)
        {
            Packet pw = new Packet(ServerMessages.MESOBAG_FAILED);
            pw.WriteByte(0);
            chr.SendPacket(pw);
        }

        public static void ReportPlayer(Character chr, Packet packet)
        {
            var characterId = packet.ReadInt();

            var reported = Server.Instance.GetCharacter(characterId);

            var reason = packet.ReadByte();

            string textReason = "Invalid reason (" + reason + ")";

            switch (reason)
            {
                case 0: textReason = "hacking"; break;
                case 1: textReason = "botting"; break;
                case 2: textReason = "scamming"; break;
                case 3: textReason = "fAKE gm"; break;
                case 4: textReason = "harassment"; break;
                case 5: textReason = "advertising"; break;
            }

            if (reported != null)
            {
                SendSueResult(reported, SueResults.YouHaveBeenSnitched);
            }
            else
            {
                // Not sending this as we have enough info anyway
                // SendSueResult(chr, SueResults.UnableToLocateTheUser);
            }

            SendSueResult(chr, SueResults.SuccessfullyReported);

            // Store the reasons somewhere...
            var report = new AbuseReport(
                chr.Name,
                chr.ID,
                chr.UserID,
                reported == null ? "null" : reported.Name, 
                reported == null ? -1 : reported.ID, 
                reported == null ? -1 : reported.UserID,
                chr.MapID,
                reason,
                textReason,
                MasterThread.CurrentDate);

            Server.Instance.ServerTraceDiscordReporter.Enqueue(report.ToString());
            MessagePacket.SendNoticeGMs(report.ToString(), MessagePacket.MessageTypes.Notice);
            ReportManager.AddAbuseReport(report);
        }

        public enum SueResults
        {
            SuccessfullyReported = 0,
            UnableToLocateTheUser = 1,
            OnlyReportOnceADay = 2,
            YouHaveBeenSnitched = 3,
            UnknownError
        }

        public static void SendSueResult(Character chr, SueResults result)
        {
            var pw = new Packet(ServerMessages.SUE_CHARACTER_RESULT);
            pw.WriteByte((byte)result);
            chr.SendPacket(pw);
        }
    }
}
