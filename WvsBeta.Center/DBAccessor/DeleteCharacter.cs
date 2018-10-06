
using System.Linq;
using MySql.Data.MySqlClient;
using WvsBeta.Common;

namespace WvsBeta.Center.DBAccessor
{
    public partial class CharacterDBAccessor
    {
        private static readonly string[] CharacterDeleteQueries =
        {
            "DELETE FROM items WHERE charid = @charid",
            "DELETE FROM inventory_eqp WHERE charid = @charid",
            "DELETE FROM inventory_bundle WHERE charid = @charid",
            "DELETE FROM character_wishlist WHERE charid = @charid",
            "DELETE FROM character_variables WHERE charid = @charid",
            "DELETE FROM character_quests WHERE charid = @charid",
            "DELETE FROM buddylist WHERE charid = @charid or buddy_charid = @charid",
            "DELETE FROM cooldowns WHERE charid = @charid",
            "DELETE FROM fame_log WHERE `from` = @charid OR `to` = @charid",
            "DELETE FROM teleport_rock_locations WHERE charid = @charid",
            "DELETE FROM skills WHERE charid = @charid",
        };

        public static byte DeleteCharacter(int accountId, int characterId)
        {
            if ((int) _characterDatabaseConnection.RunQuery(
                    "DELETE FROM characters WHERE ID = @charid AND world_id = @worldid AND userid = @userid",
                    "@charid", characterId,
                    "@worldid", CenterServer.Instance.World.ID,
                    "@userid", accountId) != 1)
            {
                // Unable to delete character
                return 10;
            }


            CharacterDeleteQueries.ForEach(query =>
                _characterDatabaseConnection.RunQuery(query, "@charid", characterId)
            );

            return 0;
        }
    }
}