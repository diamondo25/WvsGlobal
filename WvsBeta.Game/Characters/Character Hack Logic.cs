using System;
using System.Diagnostics;
using System.Linq;
using log4net;
using WvsBeta.Game.Packets;

namespace WvsBeta.Game
{
    public partial class Character
    {
        public static ILog HackLog = LogManager.GetLogger("HackLog");

        public long LastAttackPacket { get; set; }
        public byte FastAttackHackCount { get; set; }
        public DateTime HacklogMuted { get; set; }
        public int MoveTraceCount { get; set; }
        public int DesyncedSoulArrows { get; set; }
        public MovePath.MovementSource MoveTraceSource { get; set; }
        public byte OutOfMBRCount { get; set; }

        public bool AssertForHack(bool isHack, string hackType, bool seriousHack = true)
        {
            if (!isHack || IsAdmin) return false;

            HackLog.Warn(hackType);
            Trace.WriteLine(hackType);

            if (IsGM || IsAdmin) return false;
            HackLog.Warn(hackType);
            if (seriousHack && HacklogMuted < MasterThread.CurrentDate)
            {
                MessagePacket.SendNoticeGMs(
                    $"Check '{hackType}' triggered! Character: '{Name}', Map: '{MapID}'.",
                    MessagePacket.MessageTypes.Megaphone
                );
            }

            return isHack;
        }

        public enum BanReasons
        {
            Hack = 1,
            Macro = 2,
            Advertisement = 3,
            Harassment = 4,
            BadLanguage = 5,
            Scam = 6,
            Misconduct = 7,
            Sell = 8,
            ICash = 9
        }

        public void PermaBan(string reason, BanReasons banReason = BanReasons.Hack, bool doNotBanForNow = false, int extraDelay = 0)
        {
            if (IsAdmin) doNotBanForNow = true;
            if (!doNotBanForNow)
            {
                Server.Instance.AddDelayedBanRecord(this, reason, banReason, extraDelay);
            }
            else
            {
                MessagePacket.SendNoticeGMs(
                    $"Would've perma'd {Name} (uid {UserID}, cid {ID}), reason: {reason}",
                    MessagePacket.MessageTypes.Notice
                );
            }
        }

        public void TryTraceMovement(MovePath path)
        {
            if (MoveTraceCount <= 0 || MoveTraceSource != path.Source) return;
            path.Dump();
            MoveTraceCount--;
        }

        public bool IsInvalidTextInput(string inputType, string str, int maxLength = int.MaxValue, int minLength = 0)
        {
            if (AssertForHack(str.Length < minLength, $"Invalid text input '{str}' for inputType {inputType}: text not long enough (min: {minLength})", false) ||
                AssertForHack(str.Length > maxLength, $"Invalid text input '{str}' for inputType {inputType}: text too long (max: {maxLength})", false))
                return true;

            return AssertForHack(str.Any(c => c < 0x20 || c >= 0x80), $"Invalid text input '{str}' for inputType {inputType}", false);
        }
    }
}
