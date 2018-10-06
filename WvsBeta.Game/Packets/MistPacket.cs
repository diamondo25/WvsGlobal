using WvsBeta.Common.Sessions;

namespace WvsBeta.Game
{
    public static class MistPacket
    {
        public static void SendMistSpawn(Mist pMist, Character pVictim = null, short pDelay = 0)
        {
            Packet packet = new Packet(ServerMessages.AFFECTED_AREA_CREATED);
            packet.WriteInt(pMist.SpawnID);
            packet.WriteBool(pMist.MobMist);
            packet.WriteInt(pMist.SkillID);
            packet.WriteByte(pMist.SkillLevel);
            packet.WriteShort(pDelay);
            packet.WriteInt(pMist.LT_X);
            packet.WriteInt(pMist.LT_Y);
            packet.WriteInt(pMist.RB_X);
            packet.WriteInt(pMist.RB_Y);

            if (pVictim == null)
            {
                pMist.Field.SendPacket(pMist, packet);
            }
            else
            {
                pVictim.SendPacket(packet);
            }
        }

        public static void SendMistDespawn(Mist pMist)
        {
            Packet packet = new Packet(ServerMessages.AFFECTED_AREA_REMOVED);
            packet.WriteInt(pMist.SpawnID);
            pMist.Field.SendPacket(pMist, packet);
        }
    }
}