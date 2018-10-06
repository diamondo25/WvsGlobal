using WvsBeta.Common.Sessions;

namespace WvsBeta.Game
{
    public static class PartyPacket
    {
        public static Packet SendHpUpdate(int curhp, int maxhp, int otherid)
        {
            Packet pw = new Packet(ServerMessages.UPDATE_PARTYMEMBER_HP);
            pw.WriteInt(otherid);
            pw.WriteInt(curhp);
            pw.WriteInt(maxhp);
            return pw;
        }
    }
}