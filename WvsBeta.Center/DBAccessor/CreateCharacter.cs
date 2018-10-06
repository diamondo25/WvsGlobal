using System;
using WvsBeta.Common;

namespace WvsBeta.Center.DBAccessor
{
    public partial class CharacterDBAccessor
    {
        public static int CreateNewCharacter(
            int accountId, string name,
            byte gender,
            int face, int hair, int hairColor, int skin,
            byte str, byte dex, byte intt, byte luk,
            int top, int bottom, int shoes, int weapon)
        {
            bool CreateEquip(int itemid, int charid, int slot, int watk = 0, byte wdef = 0, byte slots = 7)
            {
                return (int)_characterDatabaseConnection.RunQuery(
                    "INSERT INTO inventory_eqp (charid, slot, itemid, iwatk, iwdef, slots) VALUES " +
                    "(@charid, @slot, @itemid, @iwatk, @iwdef, @slots)",
                    "@charid", charid,
                    "@slot", slot,
                    "@itemid", itemid,
                    "@iwatk", watk,
                    "@iwdef", wdef,
                    "@slots", slots
                ) == 1;
            }


            var inserted = (int)_characterDatabaseConnection.RunQuery(
                "INSERT INTO characters (name, userid, world_id, eyes, hair, skin, gender, str, dex, `int`, luk) VALUES " +
                "(@name, @userid, @worldid, @eyes, @hair, @skin, @gender, @str, @dex, @intt, @luk)",
                "@name", name,
                "@userid", accountId,
                "@worldid", CenterServer.Instance.World.ID,
                "@eyes", face,
                "@hair", hair + hairColor,
                "@skin", skin,
                "@gender", gender,
                "@str", str,
                "@dex", dex,
                "@intt", intt,
                "@luk", luk
            ) == 1;

            if (!inserted) throw new Exception("Character was not created??!");

            int characterId = _characterDatabaseConnection.GetLastInsertId();
            CreateEquip(top, characterId, -(int)Constants.EquipSlots.Slots.Top, 0, 3); // Give top
            CreateEquip(bottom, characterId, -(int)Constants.EquipSlots.Slots.Bottom, 0, 2); // Give bottom
            CreateEquip(shoes, characterId, -(int)Constants.EquipSlots.Slots.Shoe, 0, 2, 5); // Give shoes
            CreateEquip(weapon, characterId, -(int)Constants.EquipSlots.Slots.Weapon, 17, 0); // Give weapon

            return characterId;
        }
    }
}
