using WvsBeta.Database;

namespace WvsBeta.Center.DBAccessor
{
    public partial class CharacterDBAccessor
    {
        private static MySQL_Connection _characterDatabaseConnection;

        public static void InitializeDB(MySQL_Connection characterDatabaseConnection)
        {
            _characterDatabaseConnection = characterDatabaseConnection;
        }
    }
}
