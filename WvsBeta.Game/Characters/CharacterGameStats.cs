namespace WvsBeta.Game
{
    public class CharacterGameStats
    {
        public Character mCharacter { get; set; }

        public int OmokWins { get; set; }
        public int OmokTies { get; set; }
        public int OmokLosses { get; set; }

        public int MatchCardWins { get; set; }
        public int MatchCardTies { get; set; }
        public int MatchCardLosses { get; set; }

        public CharacterGameStats(Character pCharacter)
        {
            mCharacter = pCharacter;
        }

        public void Load()
        {
            /*Server.Instance.CharacterDatabase.RunQuery("SELECT * FROM gamestats WHERE id = " + mCharacter.ID.ToString());

            MySqlDataReader data = Server.Instance.CharacterDatabase.Reader;
            if (!data.HasRows)
            {
                mCharacter.GameStats.OmokWins = 0;
                mCharacter.GameStats.OmokLosses = 0;
                mCharacter.GameStats.OmokTies = 0;
                mCharacter.GameStats.MatchCardWins = 0;
                mCharacter.GameStats.MatchCardLosses = 0;
                mCharacter.GameStats.MatchCardTies = 0;
                Server.Instance.CharacterDatabase.RunQuery("INSERT INTO gamestats (id, omokwins, omoklosses, omokties, matchcardwins, matchcardties, matchcardlosses) VALUES (" + mCharacter.ID.ToString() + ", 0, 0, 0, 0, 0, 0)");
            }
            else
            {
                data.Read();
                mCharacter.GameStats.OmokWins = data.GetInt32("omokwins");
                mCharacter.GameStats.OmokTies = data.GetInt32("omokties");
                mCharacter.GameStats.OmokLosses = data.GetInt32("omoklosses");
                mCharacter.GameStats.MatchCardWins = data.GetInt32("matchcardwins");
                mCharacter.GameStats.MatchCardTies = data.GetInt32("matchcardties");
                mCharacter.GameStats.MatchCardLosses = data.GetInt32("matchcardlosses");
            }*/
        }

        public void Save()
        {
            return; // We don't use it anyway?
            Server.Instance.CharacterDatabase.RunQuery("UPDATE gamestats SET omokwins = " + mCharacter.GameStats.OmokWins + ", omokties = " + mCharacter.GameStats.OmokTies + ", omoklosses = " + mCharacter.GameStats.OmokLosses + ", matchcardwins = "
                + mCharacter.GameStats.MatchCardWins + ", matchcardties = " + mCharacter.GameStats.MatchCardTies + ", matchcardlosses = " + mCharacter.GameStats.MatchCardLosses + " WHERE id = " + mCharacter.ID.ToString());
        }
    }
}
