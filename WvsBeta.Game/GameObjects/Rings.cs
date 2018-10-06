using System.Collections.Generic;
using MySql.Data.MySqlClient;
using WvsBeta.Common;

namespace WvsBeta.Game
{
    public class Ring
    {
        public List<int> Rings { get; set; }
        public int RingID { get; set; }
        public int ItemID { get; set; }
        public int CharacterID { get; set; }
        public int PartnerID { get; set; }
        public bool Equipped { get; set; }

        public Ring(int rID, int iID, int charID, int pID, bool equipped)
        {
            Rings = new List<int>
            {
                rID
            };
            RingID = rID;
            ItemID = iID;
            CharacterID = charID;
            PartnerID = pID;
            Equipped = equipped;
        }

        public static void LoadRings(Character chr)
        {
            using (var data = (MySqlDataReader)Server.Instance.CharacterDatabase.RunQuery(
                "SELECT * FROM rings WHERE charid = @charid",
                "@charid", chr.ID
            ))
            {

                while (data.Read())
                {
                    if ((chr.Inventory.GetEquippedItemId((short)Constants.EquipSlots.Slots.Ring1, true) == 1112001) ||
                        (chr.Inventory.GetEquippedItemId((short)Constants.EquipSlots.Slots.Ring2, true) == 1112001) ||
                        (chr.Inventory.GetEquippedItemId((short)Constants.EquipSlots.Slots.Ring3, true) == 1112001) ||
                        (chr.Inventory.GetEquippedItemId((short)Constants.EquipSlots.Slots.Ring4, true) == 1112001))
                    {
                        chr.pRing = new Ring(
                            data.GetInt32("id"),
                            data.GetInt32("itemid"),
                            chr.ID,
                            data.GetInt32("partnerid"),
                            true
                        );
                    }
                    else
                    {
                        chr.pRing = new Ring(
                            data.GetInt32("id"),
                            data.GetInt32("itemid"),
                            chr.ID,
                            data.GetInt32("partnerid"),
                            false
                        );
                    }
                }
            }
        }
    }
}
