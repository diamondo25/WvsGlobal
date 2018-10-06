using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto;

namespace WvsBeta.Common.Crypto.Cryptography.Engines
{
    // This is not what MapleGlobal used.
    class NopEngine : IBlockCipher
    {
        public string AlgorithmName => "NopEngine";

        public bool IsPartialBlockOkay => false;

        public int GetBlockSize()
        {
            return 16; // Should match IV size.
        }

        public void Init(bool forEncryption, ICipherParameters parameters)
        {
        }

        public int ProcessBlock(byte[] inBuf, int inOff, byte[] outBuf, int outOff)
        {
            var processed = inBuf.Length - inOff;
            processed = Math.Min(processed, outBuf.Length - outOff);
            processed -= processed % GetBlockSize();

            Buffer.BlockCopy(inBuf, inOff, outBuf, outOff, processed);

            return processed;
        }

        void IBlockCipher.Reset()
        {
            
        }
    }
}
