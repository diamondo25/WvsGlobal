namespace WvsBeta.Game
{
    public partial class Character
    {
        public struct LevelLogRecord
        {
            public byte level { get; set; }
            // For hacking records
            public short posX { get; set; }
            public short posY { get; set; }
            public int totalMillisBetween { get; set; }
        }

        public struct SavepointLogRecord
        {
            public byte level { get; set; }
            public short posX { get; set; }
            public short posY { get; set; }
            public int totalMillisBetween { get; set; }
            public bool blocked { get; set; }
        }

        public struct PermaBanLogRecord
        {
            public string reason { get; set; }
        }

        public struct StatChangeLogRecord
        {
            public string type { get; set; }
            public int value { get; set; }
            public bool add { get; set; }
        }

        public struct MaxDamageLogRecord
        {
            public byte level { get; set; }
            public int totalStr { get; set; }
            public int totalDex { get; set; }
            public int totalInt { get; set; }
            public int totalLuk { get; set; }
            public int totalPAD { get; set; }
            public int totalMAD { get; set; }
            public int totalEva { get; set; }
            public int totalAcc { get; set; }
            public int totalCraft { get; set; }
            public int totalSpeed { get; set; }
            public int skillId { get; set; }
            public byte skillLevel { get; set; }
            public int maxDamage { get; set; }
            public int minDamage { get; set; }
            public int mobId { get; set; }
        }
    }
}