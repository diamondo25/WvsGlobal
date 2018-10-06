using System.Diagnostics;
using System.Runtime.InteropServices;
using WvsBeta.Common.Sessions;

namespace WvsBeta.Common.Character
{
    public class AvatarLook
    {
        public byte Gender { get; set; }
        public byte Skin { get; set; }
        public int Face { get; set; }
        public int PetItemId { get; set; }
        public int WeaponStickerID { get; set; }
        
        public int[] UnseenEquip { get; } = new int[Constants.EquipSlots.MaxSlotIndex];
        public int[] HairEquip { get; } = new int[Constants.EquipSlots.MaxSlotIndex];

        public void Load(GW_CharacterStat cs, int[] equips, int[] equipsCash, bool putCashInUnseen)
        {
            Gender = cs.Gender;
            Skin = cs.Skin;
            Face = cs.Face;
            HairEquip[0] = cs.Hair;

            if (putCashInUnseen)
            {
                for (byte i = 1; i < Constants.EquipSlots.MaxSlotIndex; i++)
                {
                    HairEquip[i] = equips[i];
                    UnseenEquip[i] = equipsCash[i];
                }
            }
            else
            {

                for (byte i = 1; i < Constants.EquipSlots.MaxSlotIndex; i++)
                {
                    bool isWeaponSlot = false;
                    
                    isWeaponSlot = i == (byte)Constants.EquipSlots.Slots.Weapon;

                    int equipId = 0;
                    if (!isWeaponSlot && equipsCash[i] != 0)
                    {
                        // Actual bug in Wvs: resetting HairEquip twice
                        equipId = HairEquip[i] = equipsCash[i];
                    }
                    else
                    {
                        equipId = equips[i];
                    }

                    HairEquip[i] = equipId;

                    if (!isWeaponSlot && equips[i] != 0 && equipsCash[i] != 0)
                        UnseenEquip[i] = equips[i];
                    else
                        UnseenEquip[i] = 0;
                }
            }

            WeaponStickerID = equipsCash[(byte)Constants.EquipSlots.Slots.Weapon];
        }


        public void Encode(Packet packet)
        {
            packet.WriteByte(Gender);
            packet.WriteByte(Skin);
            packet.WriteInt(Face);

            packet.WriteByte(0);
            packet.WriteInt(HairEquip[0]);
            // Note: this could use i = 0, but for the sake of clarity, we do not do that
            // Because also the client doesn't go from zero.
            for (byte i = 1; i < Constants.EquipSlots.MaxSlotIndex; i++)
            {
                int itemid = HairEquip[i];
                if (itemid == 0) continue;

                packet.WriteByte(i);
                packet.WriteInt(itemid);
            }
            packet.WriteSByte(-1);

            for (byte i = 1; i < Constants.EquipSlots.MaxSlotIndex; i++)
            {
                int itemid = UnseenEquip[i];
                if (itemid == 0) continue;

                packet.WriteByte(i);
                packet.WriteInt(itemid);
            }
            packet.WriteSByte(-1);
            
            packet.WriteInt(WeaponStickerID);

#if USE_PETITEMID
            packet.WriteInt(PetItemId);
#endif
        }

        public void Decode(Packet packet)
        {
            for (var i = HairEquip.Length - 1; i >= 0; i--)
                HairEquip[i] = 0;
            for (var i = UnseenEquip.Length - 1; i >= 0; i--)
                UnseenEquip[i] = 0;

            Gender = packet.ReadByte();
            Skin = packet.ReadByte();
            Face = packet.ReadInt();


            byte slot = 0;
            while ((slot = packet.ReadByte()) != 0xFF)
            {
                HairEquip[slot] = packet.ReadInt();
            }

            while ((slot = packet.ReadByte()) != 0xFF)
            {
                UnseenEquip[slot] = packet.ReadInt();
            }

            WeaponStickerID = packet.ReadInt();
#if USE_PETITEMID
            PetItemId = packet.ReadInt();
#endif
        }
        
    }
}
