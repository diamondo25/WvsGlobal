using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;

namespace WvsBeta.Game
{
    public class QuestData
    {
        public int ID { get; set; }
        public int QuestID { get; set; }
        public string Data { get; set; }
    }

    public class QuestMobData
    {
        public int QuestDataId { get; set; }
        public int MobID { get; set; }
        public int Killed { get; set; }
        public int Needed { get; set; }
    }

    public class CharacterQuests
    {
        private Character Character { get; set; }
        public Dictionary<int, QuestData> Quests { get; } = new Dictionary<int, QuestData>();
        public List<QuestMobData> QuestMobs { get; } = new List<QuestMobData>();

        public CharacterQuests(Character character)
        {
            Character = character;
        }

        public void SaveQuests()
        {
            int id = Character.ID;
            string query = "";

            query = "DELETE mobs.* FROM character_quest_mobs mobs LEFT JOIN character_quests quests ON mobs.id = quests.id WHERE quests.charid = " + id + "; ";
            query += "DELETE FROM character_quests WHERE charid = " + id + "; ";

            if (Quests.Count > 0)
            {
                query += "INSERT INTO character_quests (id, charid, questid, data) VALUES ";
                query += string.Join(", ", Quests.Select(kvp =>
                {
                    return "(" +
                           kvp.Value.ID + ", " +
                           id + ", " +
                           kvp.Key + ", " +
                           "'" + MySqlHelper.EscapeString(kvp.Value.Data) + "'" +
                           ")";
                }));
                query += ";";

                if (QuestMobs.Count > 0)
                {
                    query += "INSERT INTO character_quest_mobs (id, mobid, killed, needed) VALUES ";
                    query += string.Join(", ", QuestMobs.Select(kvp => "(" +
                                                                       kvp.QuestDataId + ", " +
                                                                       kvp.MobID + ", " +
                                                                       kvp.Killed + ", " +
                                                                       kvp.Needed + " " +
                                                                       ")"));
                    query += ";";
                }
            }


            Server.Instance.CharacterDatabase.RunQuery(query);
        }

        public bool LoadQuests()
        {
            using (var data = (MySqlDataReader)Server.Instance.CharacterDatabase.RunQuery(
                    "SELECT * FROM character_quests WHERE charid = @charid",
                    "@charid", Character.ID
            ))
            {
                while (data.Read())
                {
                    var qd = new QuestData()
                    {
                        ID = data.GetInt32("id"),
                        QuestID = data.GetInt32("questid"),
                        Data = data.GetString("data")
                    };
                    Quests[qd.QuestID] = qd;
                }
            }

            if (Quests.Count > 0)
            {
                using (var mdr = (MySqlDataReader)Server.Instance.CharacterDatabase.RunQuery(
                    "SELECT * FROM character_quest_mobs WHERE id IN (" + string.Join(",", Quests.Keys.ToArray()) + ")"
                ))
                {
                    while (mdr.Read())
                    {
                        var questDataId = mdr.GetInt32("id");
                        var qmd = new QuestMobData()
                        {
                            QuestDataId = questDataId,
                            MobID = mdr.GetInt32("mobid"),
                            Killed = mdr.GetInt32("killed"),
                            Needed = mdr.GetInt32("needed")
                        };
                        QuestMobs.Add(qmd);
                    }
                }
            }
            return true;
        }

        public bool AddNewQuest(int QuestID, string Data = "")
        {
            if (Quests.ContainsKey(QuestID))
                return false;

            Server.Instance.CharacterDatabase.RunQuery("INSERT INTO character_quests (id, charid, questid, data) VALUES (NULL, " + Character.ID.ToString() + ", " + QuestID + ", '" + MySqlHelper.EscapeString(Data) + "')");
            int ID = Server.Instance.CharacterDatabase.GetLastInsertId();
            
            Quests[QuestID] = new QuestData
            {
                ID = ID,
                Data = Data,
                QuestID = QuestID
            };
            QuestPacket.SendQuestDataUpdate(Character, QuestID, Data);
            return true;
        }

        public void AddOrSetQuestMob(int QuestID, int MobID, int Needed)
        {
            if (!Quests.TryGetValue(QuestID, out var qd)) return;

            var questDataId = qd.ID;

            var mobData = QuestMobs.FirstOrDefault(x => x.QuestDataId == questDataId && x.MobID == MobID);

            if (mobData == null)
            {
                QuestMobs.Add(new QuestMobData
                {
                    Killed = 0,
                    Needed = Needed,
                    MobID = MobID,
                    QuestDataId = questDataId
                });
            }
            else
            {
                mobData.Needed = Needed;
            }
        }

        public bool HasQuestMob(int QuestID, int MobID)
        {
            if (!Quests.TryGetValue(QuestID, out var qd)) return false;

            var questDataId = qd.ID;
            return QuestMobs.Exists(x => x.QuestDataId == questDataId && x.MobID == MobID);
        }

        public bool HasQuest(int QuestID)
        {
            return Quests.ContainsKey(QuestID);
        }

        public string GetQuestData(int QuestID)
        {
            return Quests.ContainsKey(QuestID) ? Quests[QuestID].Data : "";
        }

        public bool ItemCheck(int ItemID)
        {
            return false;
        }

        public void AppendQuestData(int QuestID, string pData, bool pSendPacket = true)
        {
            SetQuestData(QuestID, GetQuestData(QuestID) + pData, pSendPacket);
        }


        public void SetQuestData(int QuestID, string pData, bool pSendPacket = true)
        {
            if (!Quests.ContainsKey(QuestID)) return;

            Quests[QuestID].Data = pData;
            QuestPacket.SendQuestDataUpdate(Character, QuestID, pData);
        }

    }
}
