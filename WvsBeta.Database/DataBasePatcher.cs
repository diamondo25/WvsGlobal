using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using log4net;
using MySql.Data.MySqlClient;

namespace WvsBeta.Database
{
    public partial class DataBasePatcher
    {
        private static ILog _logger = LogManager.GetLogger(typeof(DataBasePatcher));

        private string _patchesFolder { get; set; }
        private MySQL_Connection _connection { get; set; }
        
        public static void StartPatching(MySQL_Connection connection, string patchesFolder, string name)
        {
            if (Directory.Exists(patchesFolder) == false)
            {
                _logger.Warn($"Couldn't find '{patchesFolder}'. Not patching DB.");
                return;
            }
            
            var dbPatcher = new DataBasePatcher()
            {
                _connection = connection,
                _patchesFolder = patchesFolder,
                PATCHES_TABLE = "server_evolutions_" + name
            };
            dbPatcher.SetupDB();
            var patches = dbPatcher.CreatePatchRoute();

            if (patches.Length > 0)
            {
                _logger.Info($"About to apply {patches.Length} patches...");
                dbPatcher.ApplyPatchRoute(patches);
            }
        }

        #region DB_SETUP

        private string PATCHES_TABLE;

        private string PATCHES_TABLE_CREATE_STATEMENT => $@"
CREATE TABLE `{PATCHES_TABLE}` (
   `id` int(11) NOT NULL,
   `script_up` longtext NOT NULL,
   `script_down` longtext NOT NULL,
   `file_hash` varchar(128) NOT NULL,
   `apply_date` datetime NOT NULL,
   `state` varchar(45) NOT NULL,
   `last_error` longtext NOT NULL,
   PRIMARY KEY (`id`)
 ) ENGINE=InnoDB DEFAULT CHARSET=latin1
";
        private void SetupDB()
        {
            bool foundPatchesTable = false;
            using (var tables = (MySqlDataReader)_connection.RunQuery("SHOW TABLES;"))
            {
                while (tables.Read())
                {
                    string tablename = tables.GetString(0);
                    if (tablename == PATCHES_TABLE)
                    {
                        foundPatchesTable = true;
                        break;
                    }
                }
            }

            if (!foundPatchesTable)
            {
                // Create table
                _connection.RunQuery(PATCHES_TABLE_CREATE_STATEMENT);
            }
        }


        #endregion

        #region PATCHING


        private PatchFile[] InitPatchFilesFromDisk()
        {
            return Directory
                .GetFiles(_patchesFolder, "*.sql")
                .Select(PatchFile.TryParsePatchFile)
                .Where(x => x != null)
                .OrderBy(x => x.ID)
                .ToArray();
        }

        private PatchFile[] InitPatchFilesFromDB()
        {
            using (var reader = (MySqlDataReader)_connection.RunQuery($@"SELECT * FROM `{PATCHES_TABLE}`"))
            {
                var patches = new List<PatchFile>();
                while (reader.Read()) patches.Add(PatchFile.TryParseRow(reader));

                return patches.OrderBy(x => x.ID).ToArray();
            }

        }

        class PatchRouteElem
        {
            public PatchFile PatchFile { get; set; }
            public bool Up { get; set; }
        }

        private PatchRouteElem[] CreatePatchRoute()
        {
            /*
            
            Try to create a patch route like this:
            local: 1, 2, 3, 4
            db: -
            patchroute: +1, +2, +3, +4

            local: 1
            db: 1, 2, 3
            patchroute: -3, -2

            local: 1, 2 (changed), 3
            db: 1, 2, 3
            patchroute: -3, -2, +2, +3

            */


            var patchesOnDisk = InitPatchFilesFromDisk();
            var patchesInDB = InitPatchFilesFromDB();

            var ups = new List<PatchRouteElem>();
            var downs = new List<PatchRouteElem>();

            // Validate patches
            bool reapply = false;
            var maxPatches = Math.Max(patchesOnDisk.Length, patchesInDB.Length);
            for (var i = maxPatches - 1; i >= 0; i--)
            {
                if (i >= patchesOnDisk.Length)
                {
                    // Does not exist on disk
                    downs.Add(new PatchRouteElem
                    {
                        PatchFile = patchesInDB[i],
                        Up = false,
                    });
                }
                else if (i >= patchesInDB.Length)
                {
                    // Does not exist in DB, so add
                    ups.Insert(0, new PatchRouteElem
                    {
                        PatchFile = patchesOnDisk[i],
                        Up = true,
                    });
                }
                else
                {
                    var local = patchesOnDisk[i];
                    var db = patchesInDB[i];

                    if (reapply == false && local.FileHash != db.FileHash)
                    {
                        reapply = true;
                    }

                    if (reapply)
                    {
                        // Downs should be from back to front (10 -> 9 -> 8)
                        if (db.State != PatchFile.PatchingState.AppliedDown)
                        {
                            // Only if we didn't apply it already.

                            downs.Add(new PatchRouteElem
                            {
                                PatchFile = db,
                                Up = false,
                            });
                        }

                        // Ups should be from front to back (8 -> 9 -> 10)
                        ups.Insert(0, new PatchRouteElem
                        {
                            PatchFile = local,
                            Up = true,
                        });
                    }
                }
            }

            var downsArr = downs.ToArray();
            var upsArray = ups.ToArray();

            var patchRoute = new PatchRouteElem[downs.Count + ups.Count];
            Array.Copy(downsArr, patchRoute, downsArr.Length);
            Array.Copy(upsArray, 0, patchRoute, downsArr.Length, upsArray.Length);

            return patchRoute;
        }

        private void ApplyPatchRoute(PatchRouteElem[] routeElements)
        {
            foreach (var route in routeElements)
            {
                var query = route.Up ? route.PatchFile.ScriptUp : route.PatchFile.ScriptDown;
                _logger.Debug($"Trying to patch DB. Up? {route.Up}. Query? {query}");

                if (query == null)
                {
                    _logger.Debug($"No query for patch {route.PatchFile.ID} {route.Up}, ignoring...");
                    continue;
                }

                _connection.RunQuery(
                    $@"
INSERT INTO `{PATCHES_TABLE}` VALUES 
(@id, @scriptUp, @scriptDown, @fileHash, NOW(), @state, @lastError)
ON DUPLICATE KEY UPDATE 
state = VALUES(state), 
file_hash = VALUES(file_hash),
apply_date = NOW(),
last_error = VALUES(last_error)
",
                    "@id", route.PatchFile.ID,
                    "@scriptUp", route.PatchFile.ScriptUp,
                    "@scriptDown", route.PatchFile.ScriptDown,
                    "@fileHash", route.PatchFile.FileHash,
                    "@state", (route.Up ? PatchFile.PatchingState.ApplyingUp : PatchFile.PatchingState.ApplyingDown).ToString(),
                    "@lastError", ""
                );

                try
                {
                    _connection.RunQuery(query);
                }
                catch (Exception ex)
                {
                    var dbException = new DataBasePatchException("Exception while applying patch", query, ex);
                    _logger.Fatal(dbException);

                    _connection.RunQuery(
                        $@"
UPDATE `{PATCHES_TABLE}` SET
last_error = @lastError
WHERE id = @id
",
                        "@id", route.PatchFile.ID,
                        "@lastError", ex.ToString()
                    );

                    throw dbException;
                }

                _logger.Info($"Applied patch {route.PatchFile.ID}!");

                _connection.RunQuery(
                    $@"
UPDATE `{PATCHES_TABLE}` SET
state = @state,
file_hash = @fileHash,
script_up = @scriptUp,
script_down = @scriptDown,
apply_date = NOW()
WHERE id = @id
",
                    "@id", route.PatchFile.ID,
                    "@fileHash", route.PatchFile.FileHash,
                    "@scriptUp", route.PatchFile.ScriptUp,
                    "@scriptDown", route.PatchFile.ScriptDown,
                    "@state", (route.Up ? PatchFile.PatchingState.AppliedUp : PatchFile.PatchingState.AppliedDown).ToString()
                );
            }
        }

        #endregion
    }
}
