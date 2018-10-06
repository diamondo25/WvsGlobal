using WvsBeta.Common.Sessions;

namespace WvsBeta.Game
{
    public static class ReactorPacket
    {
        public static void ShowReactor(Reactor reactor, bool toChar = false, Character chr = null)
        {
            Packet packet = new Packet(ServerMessages.REACTOR_ENTER_FIELD);
            packet.WriteShort(reactor.ID);
            packet.WriteByte(reactor.State);
            packet.WriteShort(reactor.X);
            packet.WriteShort(reactor.Y);
            packet.WriteByte(reactor.Z);
            packet.WriteByte(reactor.ZM);

            if (toChar && chr != null)
                chr.SendPacket(packet);
            else
                reactor.Field.SendPacket(packet);
        }

        public static void ReactorChangedState(Reactor reactor)
        {
            Packet packet = new Packet(ServerMessages.REACTOR_CHANGE_STATE);
            packet.WriteShort(reactor.ID);
            packet.WriteByte(reactor.State); //State
            packet.WriteShort(reactor.X);
            packet.WriteShort(reactor.Y);
            packet.WriteShort(3);
            packet.WriteByte(0);
            packet.WriteByte(5); //Frame delay ?
            reactor.Field.SendPacket(packet);
        }

        public static void DestroyReactor(Reactor reactor)
        {
            Packet packet = new Packet(ServerMessages.REACTOR_LEAVE_FIELD);
            packet.WriteShort(reactor.ID);
            MasterThread.RepeatingAction.Start("dr-" + reactor.Field.ID + "-" + reactor.ID, time => reactor.Field.SendPacket(packet), 650, 0);
        }

        public static void HandleReactorHit(Character chr, Packet packet)
        {
            byte rid = packet.ReadByte();
            byte direction = packet.ReadByte();
            chr.Field.PlayerHitReactor(chr, rid);
        }
    }
}
