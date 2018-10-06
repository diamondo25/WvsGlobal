using WvsBeta.Common;
using WvsBeta.Common.Character;

namespace WvsBeta.Center.DBAccessor
{
    public partial class CharacterDBAccessor
    {
        public static AvatarData LoadAvatar(int characterId)
        {
            var ad = new AvatarData();

            ad.CharacterStat = GetCharacterData(characterId);

            var equips = new int[Constants.EquipSlots.MaxSlotIndex];
            var equipsCash = new int[Constants.EquipSlots.MaxSlotIndex];

            foreach (var (itemid, slot) in GetEquippedItemID(characterId))
            {
                if (Constants.EquipSlots.IsValidEquipSlot(slot) == false) continue;

                if (slot > 100)
                    equipsCash[(short)(slot - 100)] = itemid;
                else
                    equips[slot] = itemid;
            }


            ad.AvatarLook = new AvatarLook();
            ad.AvatarLook.Load(ad.CharacterStat, equips, equipsCash, true);

            return ad;
        }
    }
}
