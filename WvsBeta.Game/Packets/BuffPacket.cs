using WvsBeta.Common.Sessions;

namespace WvsBeta.Game
{
    public static class BuffPacket
    {
        public static void AddMapBuffValues(Character chr, Packet pw, BuffValueTypes pBuffFlags = BuffValueTypes.ALL)
        {
            CharacterPrimaryStats ps = chr.PrimaryStats;
            long currentTime = MasterThread.CurrentTime;
            BuffValueTypes added = 0;

            int tmp = pw.Position;
            pw.WriteUInt(0); // Placeholder

            ps.BuffSpeed.EncodeForRemote(ref added, currentTime, stat => pw.WriteByte((byte)stat.N), pBuffFlags);
            ps.BuffComboAttack.EncodeForRemote(ref added, currentTime, stat => pw.WriteByte((byte)stat.N), pBuffFlags);
            ps.BuffCharges.EncodeForRemote(ref added, currentTime, stat => pw.WriteInt(stat.R), pBuffFlags);
            ps.BuffStun.EncodeForRemote(ref added, currentTime, stat => pw.WriteInt(stat.R), pBuffFlags);
            ps.BuffDarkness.EncodeForRemote(ref added, currentTime, stat => pw.WriteInt(stat.R), pBuffFlags);
            ps.BuffSeal.EncodeForRemote(ref added, currentTime, stat => pw.WriteInt(stat.R), pBuffFlags);
            ps.BuffWeakness.EncodeForRemote(ref added, currentTime, stat => pw.WriteInt(stat.R), pBuffFlags);
            ps.BuffPoison.EncodeForRemote(ref added, currentTime, stat =>
            {
                pw.WriteShort(stat.N);
                pw.WriteInt(stat.R);
            }, pBuffFlags);
            ps.BuffSoulArrow.EncodeForRemote(ref added, currentTime, null, pBuffFlags);
            ps.BuffShadowPartner.EncodeForRemote(ref added, currentTime, null, pBuffFlags);
            ps.BuffDarkSight.EncodeForRemote(ref added, currentTime, null, pBuffFlags);

            pw.SetUInt(tmp, (uint)added);
        }

        public static void SetTempStats(Character chr, BuffValueTypes pFlagsAdded, short pDelay = 0)
        {
            if (pFlagsAdded == 0) return;
            Packet pw = new Packet(ServerMessages.FORCED_STAT_SET);
            chr.PrimaryStats.EncodeForLocal(pw, pFlagsAdded);
            pw.WriteShort(pDelay);
            if ((pFlagsAdded & BuffValueTypes.SPEED_BUFF_ELEMENT) != 0)
            {
                pw.WriteByte(0); // FIX: This should be the 'movement info index'
            }
            chr.SendPacket(pw);
        }

        public static void ResetTempStats(Character chr, BuffValueTypes removedFlags)
        {
            if (removedFlags == 0) return;

            Packet pw = new Packet(ServerMessages.FORCED_STAT_RESET);
            pw.WriteUInt((uint)removedFlags);
            if ((removedFlags & BuffValueTypes.SPEED_BUFF_ELEMENT) != 0)
            {
                pw.WriteByte(0); // FIX: This should be the 'movement info index'
            }
            chr.SendPacket(pw);
        }
    }
}