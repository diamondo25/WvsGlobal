using System;

namespace WvsBeta.Common
{
    public class Rand32
    {
        #region Static Members
        internal static Rand32 GlobalRnd = new Rand32();
        
        public static uint Next() => GlobalRnd.Random();
        public static int NextBetween(int min = 0, int max = int.MaxValue) => GlobalRnd.ValueBetween(min, max);
        #endregion

        #region Public Members
        private uint Seed1;
        private uint Seed2;
        private uint Seed3;
        private uint PastSeed1;
        private uint PastSeed2;
        private uint PastSeed3;

        public Rand32()
        {
            uint Seed = (uint)(1170746341 * Environment.TickCount - 755606699);
            SetSeed(Seed, Seed, Seed);
        }

        public void SetSeed(uint Seed1, uint Seed2, uint Seed3)
        {
            this.Seed1 = Seed1 | 0x100000;
            this.PastSeed1 = Seed1 | 0x100000;
            this.Seed2 = Seed2 | 0x1000;
            this.PastSeed2 = Seed2 | 0x1000;
            this.Seed3 = Seed3 | 0x10;
            this.PastSeed3 = Seed3 | 0x10;
        }

        public uint Random()
        {
            PastSeed1 = Seed1;
            PastSeed2 = Seed2;
            PastSeed3 = Seed3;

            Seed1 = ((Seed1 & 0xFFFFFFFE) << 12) ^ ((Seed1 & 0x7FFC0 ^ (Seed1 >> 13)) >> 6);
            Seed2 = 16 * (Seed2 & 0xFFFFFFF8) ^ (((Seed2 >> 2) ^ Seed2 & 0x3F800000) >> 23);
            Seed3 = ((Seed3 & 0xFFFFFFF0) << 17) ^ (((Seed3 >> 3) ^ Seed3 & 0x1FFFFF00) >> 8);

            return Seed1 ^ Seed2 ^ Seed3;
        }

        public int NextSeedINT()
        {
            return (int)Random();
        }

        public int ValueBetween(int min = 0, int max = int.MaxValue)
        {
            if (max == 0) return 0;
            long inval = Random() % (max - min);
            inval += min;
            return (int)Math.Max(min, Math.Min(inval, max));
        }
        #endregion
    }
}
