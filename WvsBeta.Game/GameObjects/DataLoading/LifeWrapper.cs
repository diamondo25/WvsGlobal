namespace WvsBeta.Game
{
    public class LifeWrapper
    {
        private Life _life;

        public int ID => _life.ID;
        public int RespawnTime => _life.RespawnTime;
        public char Type => _life.Type;
        public ushort Foothold => _life.Foothold;
        public bool FacesLeft => _life.FacesLeft;
        public short X => _life.X;
        public short Y => _life.Y;
        public short Cy => _life.Cy;
        public short Rx0 => _life.Rx0;
        public short Rx1 => _life.Rx1;

        public LifeWrapper(Life life)
        {
            _life = life; // da daa da da da.
        }
    }
}