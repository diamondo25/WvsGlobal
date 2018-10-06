using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace WvsBeta.Center.DBAccessor
{
    public partial class CharacterDBAccessor
    {
        public static IEnumerable<(int itemId, short slot)> GetEquippedItemID(int characterId)
        {
            using (var reader = _characterDatabaseConnection.RunQuery(
                @"
SELECT itemid, ABS(slot) FROM inventory_eqp WHERE charid = @charid AND slot < 0
UNION
SELECT itemid, ABS(slot) FROM itemlocker WHERE characterid = @charid AND slot < 0
",
                "@charid", characterId
            ) as MySqlDataReader)
            {
                while (reader.Read())
                {
                    yield return (reader.GetInt32(0), reader.GetInt16(1));
                }
            }

        }
    }
}
