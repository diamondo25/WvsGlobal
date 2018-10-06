using MySql.Data.MySqlClient;

namespace WvsBeta.Center.DBAccessor
{
    public partial class CharacterDBAccessor
    {
        public static (int worldRank, int worldRankMove, int jobRank, int jobRankMove)? LoadRank(int characterId)
        {
            using (var reader = _characterDatabaseConnection.RunQuery(
                @"
SELECT 
    world_cpos AS world_rank, 
    world_cpos - world_opos AS world_rank_move, 
    job_cpos AS job_rank, 
    job_cpos - job_opos AS job_rank_move 
FROM characters 
WHERE 
    id = @charid AND 
    world_cpos > 0 AND 
    job_cpos > 0 AND 
    FLOOR(job / 100) <> 5
",
                "@charid", characterId
            ) as MySqlDataReader)
            {
                if (!reader.Read()) return null;

                return (
                    reader.GetInt32("world_rank"),
                    reader.GetInt32("world_rank_move"),
                    reader.GetInt32("job_rank"),
                    reader.GetInt32("job_rank_move")
                );

            }
        }
    }
}
