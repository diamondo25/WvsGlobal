using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WvsBeta.Common
{
    public class Cryptos
    {

        public static string SHA512_ComputeHexaHash(string text)
        {
            // Gets the SHA512 hash for text

            using (var SHhash = SHA512.Create())
            {
                byte[] data = Encoding.ASCII.GetBytes(text);
                byte[] hash = SHhash.ComputeHash(data);

                var hexaHash = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash)
                {
                    hexaHash.AppendFormat("{0:x2}", b);
                }

                return hexaHash.ToString();
            }
        }

        static Random rnd = new Random();
        public static string GetNewSessionHash()
        {
            var bytes = new byte[16];
            rnd.NextBytes(bytes);

            return string.Join("", bytes.Select(x => x.ToString("X2")));
        }

    }
}
