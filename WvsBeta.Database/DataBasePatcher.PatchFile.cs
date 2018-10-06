using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using MySql.Data.MySqlClient;

namespace WvsBeta.Database
{
    public partial class DataBasePatcher
    {
        class PatchFile
        {
            public int ID { get; private set; }
            public string ScriptUp { get; private set; }
            public string ScriptDown { get; private set; }
            public string FileHash { get; private set; }
            public PatchingState State { get; private set; }

            public enum PatchingState
            {
                UNKNOWN,
                ApplyingUp,
                ApplyingDown,
                AppliedUp,
                AppliedDown,
                LocalFile
            }

            enum ParsingState
            {
                JustStarted,
                ScriptUp,
                ScriptDown,
            }

            private const string SEPERATOR_SCRIPT_UP = "------- UP -------";
            private const string SEPERATOR_SCRIPT_DOWN = "------- DOWN -------";

            private static string CleanupString(string input)
            {
                if (input == null) return null;
                input = input.Trim('\r', '\n', '\t', ' ');
                return input.Length == 0 ? null : input;
            }

            private void CalculateFileHash()
            {
                using (var SHhash = SHA512.Create())
                {
                    byte[] hash = SHhash.ComputeHash(Encoding.ASCII.GetBytes("" + ScriptUp + ScriptDown));

                    var hexaHash = new StringBuilder(hash.Length * 2);

                    foreach (byte b in hash)
                    {
                        hexaHash.AppendFormat("{0:x2}", b);
                    }

                    FileHash = hexaHash.ToString();
                }
            }

            public static PatchFile TryParsePatchFile(string filename)
            {
                if (Path.GetExtension(filename) != ".sql") return null;

                var lines = File.ReadAllLines(filename);

                var currentState = ParsingState.JustStarted;

                var pf = new PatchFile();

                foreach (var line in lines)
                {
                    if (line == SEPERATOR_SCRIPT_UP)
                    {
                        currentState = ParsingState.ScriptUp;
                        continue;
                    }
                    if (line == SEPERATOR_SCRIPT_DOWN)
                    {
                        currentState = ParsingState.ScriptDown;
                        continue;
                    }


                    switch (currentState)
                    {
                        case ParsingState.JustStarted:
                            // Ignoring line...
                            continue;

                        case ParsingState.ScriptDown:
                            pf.ScriptDown += line + "\r\n";
                            break;

                        case ParsingState.ScriptUp:
                            pf.ScriptUp += line + "\r\n";
                            break;
                    }
                }

                if (currentState == ParsingState.JustStarted)
                    throw new Exception("Did not find seperators. Script: " + filename);

                pf.ScriptUp = CleanupString(pf.ScriptUp);
                pf.ScriptDown = CleanupString(pf.ScriptDown);

                if (pf.ScriptUp == null && pf.ScriptDown == null)
                    throw new Exception("Did not initialize a Down or Up script. Script: " + filename);

                if (pf.ScriptDown != null && pf.ScriptUp == null)
                    throw new Exception("Found Down script, but no Up script. Script: " + filename);

                pf.ID = int.Parse(Path.GetFileNameWithoutExtension(filename));
                pf.CalculateFileHash();
                pf.State = PatchingState.LocalFile;
                
                return pf;
            }


            public static PatchFile TryParseRow(MySqlDataReader dataReader)
            {
                PatchingState ps;

                if (Enum.TryParse(dataReader.GetString("state"), out ps) == false)
                    ps = PatchingState.UNKNOWN;

                return new PatchFile
                {
                    ID = dataReader.GetInt32("id"),
                    ScriptUp = dataReader.GetString("script_up"),
                    ScriptDown = dataReader.GetString("script_down"),
                    FileHash = dataReader.GetString("file_hash"),
                    State = ps
                };
            }
        }
    }
}
