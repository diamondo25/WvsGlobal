using WvsBeta.Common.Sessions;

namespace WvsBeta.Game
{
    public static class CoconutPackets
    {
        public static void HandleEvent(Character chr, Packet packet)
        {
            short CoconutID = packet.ReadShort();
            short CharStance = packet.ReadShort();
            HitCoconut(chr, CoconutID, CharStance);

        }

        public static void CoconutScore(Character chr, short maple, short story)
        {
            // was 157 in v40b, assumed to be 160 in v12

            Packet pw = new Packet(ServerMessages.COCONUT_SCORE); // 157, pressumably 160 in v12
            pw.WriteShort(maple);
            pw.WriteShort(story);
            chr.Field.SendPacket(pw, chr, false);
        }

        public static void SpawnCoconut(Character chr, bool spawn, int id, int type)
        {
            Packet pw = new Packet(ServerMessages.COCONUT_HIT);
            pw.WriteShort(0); //Coconut ID
            pw.WriteShort(0); //Type of hit?
            pw.WriteByte(0); //0 = spawn 1 = hit 
            chr.Field.SendPacket(pw, chr, false);
        }

        public static void HitCoconut(Character chr, short cID, short Stance)
        {
            Packet pw = new Packet(ServerMessages.COCONUT_HIT);
            pw.WriteShort(cID); //Coconut ID
            pw.WriteShort(Stance); //Delay! lol
            pw.WriteByte(1); //0 = spawn 1 = hit 2 = break 3 = destroy
            chr.SendPacket(pw);
        }

        public static void ForcedEquip(Character chr, byte team)
        {
            Packet pw = new Packet(ServerMessages.FIELD_SPECIFIC_DATA); // 44, pressumably 47 in v12
            pw.WriteByte(team); //0 : red, 1 : blue
            chr.Field.SendPacket(pw);
        }


    }
}
