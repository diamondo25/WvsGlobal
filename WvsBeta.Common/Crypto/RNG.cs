using System;
using System.Security.Cryptography;

public static class RNG
{
    private static readonly RNGCryptoServiceProvider RNGCryptoEngine = new RNGCryptoServiceProvider();

    public static class EightDecimals
    {
        public static double generate()
        {
            byte[] eightBytes = new byte[8];


            RNGCryptoEngine.GetBytes(eightBytes);

            return eightBytes[0] % 10 * 0.1 +
                    eightBytes[1] % 10 * 0.01 +
                    eightBytes[2] % 10 * 0.001 +
                    eightBytes[3] % 10 * 0.0001 +
                    eightBytes[4] % 10 * 0.00001 +
                    eightBytes[5] % 10 * 0.000001 +
                    eightBytes[6] % 10 * 0.0000001 +
                    eightBytes[7] % 10 * 0.00000001;
        }

        //public static float generateSteal(float chanceToSucceed)//Will work with later
        //{
        //    if(chanceToSucceed > 1.0f)
        //        throw new ArgumentException("The chance to succeed on stealing is greater than 100% (i.e. chanceToSucceed > 1.0f)");
        //
        //    return generate() * (2.0f - chanceToSucceed);
        //}
    }

    public static class Range
    {
        public static long generate(long min, long max, bool inclusive)
        {
            byte[] eightBytes = new byte[8];


            if (min == max)
                return min;
            else if (min > max)
            {
                long temp = min;
                min = max;
                max = temp;
            }
            RNGCryptoEngine.GetBytes(eightBytes);
            ulong convertedBits = BitConverter.ToUInt64(eightBytes, 0);

            if (inclusive) max++;

            if (min < 0)
                if (max < 0)
                {
                    ulong uMax = (ulong)-max;
                    return -(long)(convertedBits % ((ulong)-min - uMax) + uMax) - 1;
                }
                else
                {
                    ulong uMin = (ulong)-min;
                    return (long)(convertedBits % ((ulong)max + uMin) - uMin);//unlike uint, the ulong cast in this line is necessary
                }
            return (long)((ulong)min + convertedBits % (ulong)(max - min));//unlike uint, the ulong casts in this line are necessary
        }

        public static int generate(int min, int max, bool inclusive = false)
        {
            byte[] fourBytes = new byte[4];


            if (min == max)
                return min;
            else if (min > max)
            {
                int temp = min;
                min = max;
                max = temp;
            }
            RNGCryptoEngine.GetBytes(fourBytes);
            uint convertedBits = BitConverter.ToUInt32(fourBytes, 0);

            if (inclusive) max++;

            if (min < 0)
                if (max < 0)
                {
                    uint uMax = (uint)-max;
                    return -(int)(convertedBits % ((uint)-min - uMax) + uMax) - 1;
                }
                else
                {
                    uint uMin = (uint)-min;
                    return (int)(convertedBits % ((uint)max + uMin) - uMin);//uint cast in this line is an optimization to avoid a long
                }
            return (int)((uint)min + convertedBits % (uint)(max - min));//uint casts in this line are an optimization to avoid a long
        }
        
        public static short generate(short min, short max, bool inclusive = false)
        {
            byte[] twoBytes = new byte[2];


            if (min == max)
                return min;
            else if (min > max)
            {
                short temp = min;
                min = max;
                max = temp;
            }
            RNGCryptoEngine.GetBytes(twoBytes);
            if (inclusive) max++;

            return (short)(min + BitConverter.ToUInt16(twoBytes, 0) % (max - min));
        }

        public static sbyte generate(sbyte min, sbyte max, bool inclusive = false)
        {
            byte[] oneByte = new byte[1];


            if (min == max)
                return min;
            else if (min > max)
            {
                sbyte temp = min;
                min = max;
                max = temp;
            }
            RNGCryptoEngine.GetBytes(oneByte);
            if (inclusive) max++;

            return (sbyte)(min + oneByte[0] % (max - min));
        }

        public static ulong generate(ulong min, ulong max, bool inclusive = false)
        {
            byte[] eightBytes = new byte[8];


            if (min == max)
                return min;
            else if (min > max)
            {
                ulong temp = min;
                min = max;
                max = temp;
            }
            RNGCryptoEngine.GetBytes(eightBytes);
            if (inclusive) max++;

            return min + BitConverter.ToUInt64(eightBytes, 0) % (max - min);
        }

        public static uint generate(uint min, uint max, bool inclusive = false)
        {
            byte[] fourBytes = new byte[4];


            if (min == max)
                return min;
            else if (min > max)
            {
                uint temp = min;
                min = max;
                max = temp;
            }
            RNGCryptoEngine.GetBytes(fourBytes);
            if (inclusive) max++;

            return min + BitConverter.ToUInt32(fourBytes, 0) % (max - min);
        }

        public static ushort generate(ushort min, ushort max, bool inclusive = false)
        {
            byte[] twoBytes = new byte[2];


            if (min == max)
                return min;
            else if (min > max)
            {
                ushort temp = min;
                min = max;
                max = temp;
            }
            RNGCryptoEngine.GetBytes(twoBytes);
            if (inclusive) max++;

            return (ushort)(min + BitConverter.ToUInt16(twoBytes, 0) % (max - min));
        }

        public static byte generate(byte min, byte max, bool inclusive = false)
        {
            byte[] oneByte = new byte[1];


            if (min == max)
                return min;
            else if (min > max)
            {
                byte temp = min;
                min = max;
                max = temp;
            }
            RNGCryptoEngine.GetBytes(oneByte);
            if (inclusive) max++;

            return (byte)(min + oneByte[0] % (max - min));
        }
    }
}