using MySql.Data.MySqlClient;

namespace WvsBeta.Center.DBAccessor
{
    public partial class CharacterDBAccessor
    {
        public static void UpdateRank(int characterId)
        {
            using (var reader = _characterDatabaseConnection.RunQuery(
                @"
UPDATE
    characters
SET
    world_opos = world_cpos,
    job_opos = job_cpos
WHERE 
    id = @charid
",
                "@charid", characterId
            ) as MySqlDataReader)
            {
            }
        }
    }
}
