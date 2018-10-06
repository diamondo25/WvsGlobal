using MySql.Data.MySqlClient;
using WvsBeta.Common.Sessions;

namespace WvsBeta.Game
{
    public static class FamePacket
    {
        public static void HandleFame(Character chr, Packet pr)
        {
            int charId = pr.ReadInt();
            bool up = pr.ReadBool();


            Character victim = chr.Field.GetPlayer(charId);

            if (charId == chr.ID)
            {
                return;
            }
            else if (victim == null)
            {
                SendFameError(chr, 0x01); // Incorrect User error
                return;
            }
            else if (chr.PrimaryStats.Level < 15)
            {
                SendFameError(chr, 0x02); // Level under 15
                return;
            }


            using (var reader = Server.Instance.CharacterDatabase.RunQuery("SELECT 1 FROM `fame_log` WHERE `from` = @from AND `to` = @to AND time >= DATE_SUB(NOW(), INTERVAL 1 MONTH)", "@from", chr.ID, "@to", charId) as MySqlDataReader)
            {
                if (reader.Read())
                {
                    // Already famed this person this month
                    SendFameError(chr, 0x04);
                    return;
                }
            }

            using (var reader = Server.Instance.CharacterDatabase.RunQuery("SELECT 1 FROM `fame_log` WHERE `from` = @from AND time >= DATE_SUB(NOW(), INTERVAL 1 DAY)", "@from", chr.ID) as MySqlDataReader)
            {
                if (reader.Read())
                {
                    // Already famed today
                    SendFameError(chr, 0x03);
                    return;
                }
            }

            victim.AddFame((short)(up ? 1 : -1));
            Server.Instance.CharacterDatabase.RunQuery("INSERT INTO fame_log (`from`, `to`, `time`) VALUES (@from, @to, NOW());", "@from", chr.ID, "@to", victim.ID);
            SendFameSucceed(chr, victim, up);

            /*
            // Check fame records
            using (var reader = Server.Instance.CharacterDatabase.RunQuery(@"
SELECT 
    GROUP_CONCAT(from_char.name) 
FROM fame_log fl 
JOIN characters from_char ON from_char.id = fl.`from` 
WHERE 1 AND
    fl.`to` = @to_id AND
    
GROUP BY fl.`to`
HAVING COUNT(*) > 5
",
"@to_id", victim.ID) as MySqlDataReader)
            {

            }
            */
            if (!up)
            {
                Server.Instance.ServerTraceDiscordReporter.Enqueue(
                    $"{chr.Name} ({chr.ID}) defamed {victim.Name} ({victim.ID}) in map {chr.MapID}"
                );
            }
        }

        //1 -> user incorrectly entered
        //2 -> users under 15 unable to toggle fame
        //3 -> can't raise or drop anymore today
        //4 -> can't raise or drop that person this month
        //6 -> fame not changed due to unk error
        public static void SendFameError(Character chr, byte error)
        {
            Packet pw = new Packet(ServerMessages.GIVE_POPULARITY_RESULT);
            pw.WriteByte(error);
            chr.SendPacket(pw);
        }

        //0 -> you have raised/lowered X's level of fame
        //5 -> X has raised/lowered Y's level of fame
        public static void SendFameSucceed(Character chr, Character victim, bool up)
        {
            Packet pw = new Packet(ServerMessages.GIVE_POPULARITY_RESULT);
            pw.WriteByte(5);
            pw.WriteString(chr.Name);
            pw.WriteBool(up);
            victim.SendPacket(pw);

            pw = new Packet(ServerMessages.GIVE_POPULARITY_RESULT);
            pw.WriteByte(0);
            pw.WriteString(victim.Name);
            pw.WriteBool(up);
            pw.WriteInt(victim.PrimaryStats.Fame);
            chr.SendPacket(pw);
        }
    }
}
