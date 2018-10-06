using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace WvsBeta.Center.DBAccessor
{
    public partial class CharacterDBAccessor
    {
        public static IEnumerable<int> GetCharacterIdList(int accountId)
        {
            using (var reader = _characterDatabaseConnection.RunQuery(
                "SELECT id FROM characters WHERE userid = @userId",
                "@userId", accountId
            ) as MySqlDataReader)
            {
                List<Tuple<int, short>> items = new List<Tuple<int, short>>();

                while (reader.Read())
                {
                    yield return reader.GetInt32("id");
                }
            }

        }
    }
}
