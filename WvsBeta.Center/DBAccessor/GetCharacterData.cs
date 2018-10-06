using System;
using MySql.Data.MySqlClient;
using WvsBeta.Common.Character;

namespace WvsBeta.Center.DBAccessor
{
    public partial class CharacterDBAccessor
    {
        public static GW_CharacterStat GetCharacterData(int characterId)
        {
            using (var reader = _characterDatabaseConnection.RunQuery(
                "SELECT * FROM characters WHERE id = @id",
                "@id", characterId
            ) as MySqlDataReader)
            {
                if (!reader.Read()) throw new Exception("Character does not exist!");

                var cs = new GW_CharacterStat();
                cs.LoadFromReader(reader);

                return cs;
            }
        }
    }
}
