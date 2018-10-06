using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;

namespace WvsBeta.Game
{
    public class CharacterVariables
    {
        private Character Character { get; set; }
        private Dictionary<string, string> Variables { get; set; }

        public CharacterVariables(Character character)
        {
            Character = character;
            Variables = new Dictionary<string, string>();
        }

        public void Save()
        {
            int id = Character.ID;
            string query = "";
            
            query += "DELETE FROM character_variables WHERE charid = " + id + "; ";
            if (Variables.Count > 0)
            {
                query += "INSERT INTO character_variables (charid, `key`, `value`) VALUES ";
                query += string.Join(",", Variables.Select(kvp =>
                    "(" +
                    id + ", " +
                    "'" + MySqlHelper.EscapeString(kvp.Key) + "', " +
                    "'" + MySqlHelper.EscapeString(kvp.Value) + "'" +
                    ")"
                ));
                query += ";";
            }
            
            Server.Instance.CharacterDatabase.RunQuery(query);
            
        }

        public bool Load()
        {
            using (var data = (MySqlDataReader)Server.Instance.CharacterDatabase.RunQuery("SELECT * FROM character_variables WHERE charid = " + Character.ID))
            {
                if (!data.HasRows)
                {
                    return false;
                }
                else
                {
                    while (data.Read())
                    {
                        Variables.Add(data.GetString("key"), data.GetString("value"));
                    }
                    return true;
                }
            }
        }

        public string GetVariableData(string pName)
        {
            if (Variables.ContainsKey(pName))
            {
                return Variables[pName];
            }
            return null;
        }

        public void SetVariableData(string pName, string pVariableData)
        {
            if (pVariableData == null) return;

            if (!Variables.ContainsKey(pName)) Variables.Add(pName, pVariableData);
            else Variables[pName] = pVariableData;
        }

        public List<string> GetVariableDataList(string pName)
        {
            return SplitData(GetVariableData(pName));
        }

        public void SetVariableDataList(string pName, List<string> pVariableDataList)
        {
            if (pVariableDataList == null) return;

            if (!Variables.ContainsKey(pName)) Variables.Add(pName, JoinData(pVariableDataList));
            else Variables[pName] = JoinData(pVariableDataList);
        }

        public bool RemoveVariable(string pName)
        {
            if (Variables.ContainsKey(pName))
            {
                Variables.Remove(pName);
                return true;
            }
            return false;
        }

        public static List<string> SplitData(string pVariableData)
        {
            if (pVariableData == null) return null;
            return pVariableData.Split(';').ToList();
        }

        public static string JoinData(List<string> pDataList)
        {
            if (pDataList == null) return null;
            return string.Join(";", pDataList);
        }
    }
}