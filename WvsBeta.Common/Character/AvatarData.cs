using WvsBeta.Common.Sessions;

namespace WvsBeta.Common.Character
{
    public class AvatarData
    {
        public GW_CharacterStat CharacterStat { get; set; }
        public AvatarLook AvatarLook { get; set; }

        public void Encode(Packet packet)
        {
            CharacterStat.Encode(packet);

            // Newer versions do not really use this 
#if USING_AVATARLOOK_ENCODE
            AvatarLook.Encode(packet);
#else
            
            for (byte i = 1; i < Constants.EquipSlots.MaxSlotIndex; i++)
            {
                int itemid = AvatarLook.HairEquip[i];
                if (itemid == 0) continue;

                packet.WriteByte(i);
                packet.WriteInt(itemid);
            }
            // Client checks for '!((byte)slot)', so this must be zero!
            packet.WriteByte(0);


            for (byte i = 1; i < Constants.EquipSlots.MaxSlotIndex; i++)
            {
                int itemid = AvatarLook.UnseenEquip[i];
                if (itemid == 0) continue;

                packet.WriteByte(i);
                packet.WriteInt(itemid);
            }
            packet.WriteByte(0);
#endif
        }

        public void Decode(Packet packet)
        {
            CharacterStat = new GW_CharacterStat();
            CharacterStat.Decode(packet);

            AvatarLook = new AvatarLook();
            // Newer versions do not really use this 
#if USING_AVATARLOOK_ENCODE
            AvatarLook.Decode(packet);
#else
            for (var i = 0; i < Constants.EquipSlots.MaxSlotIndex; i++)
            {
                AvatarLook.HairEquip[i] = 0;
                AvatarLook.UnseenEquip[i] = 0;
            }

            byte slot = 0;

            while ((slot = packet.ReadByte()) != 0)
            {
                int itemid = packet.ReadInt();
                AvatarLook.HairEquip[slot] = itemid;
            }

            while ((slot = packet.ReadByte()) != 0)
            {
                int itemid = packet.ReadInt();
                AvatarLook.UnseenEquip[slot] = itemid;
            }
#endif
        }
    }

}