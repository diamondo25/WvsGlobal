using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace WvsBeta.Common.Sessions
{
    public class Packet : IDisposable
    {
        private MemoryStream _memoryStream;
        private BinaryReader _binReader;
        private BinaryWriter _binWriter;

        public MemoryStream MemoryStream => _memoryStream;

        /// <summary>
        /// Millis when this packet has been built. Use for timing 'critical' things, aside from MasterThread.CurrentTime (updated every loop)
        /// </summary>
        public long PacketCreationTime { get; } = (long)((Stopwatch.GetTimestamp() * (1.0 / Stopwatch.Frequency)) * 1000.0);

        public byte Opcode { get; private set; }

        public Packet(byte[] pData) : this(pData, pData.Length) { }

        public Packet(byte[] pData, int length)
        {
            _memoryStream = new MemoryStream(pData, 0, length, false);
            _binReader = new BinaryReader(_memoryStream);

            Opcode = ReadByte();
            Position = 0;
        }

        /// <summary>
        /// Initialize a Packet from a compressed GZip stream
        /// </summary>
        /// <param name="gzipStream">Compressed GZip stream</param>
        public Packet(GZipStream gzipStream)
        {
            _memoryStream = new MemoryStream();
            gzipStream.CopyTo(_memoryStream);
            _memoryStream.Position = 0;
            _binReader = new BinaryReader(_memoryStream);


            Opcode = ReadByte();
            Position = 0;
        }

        public Packet(DeflateStream deflateStream)
        {
            _memoryStream = new MemoryStream();
            deflateStream.CopyTo(_memoryStream);
            _memoryStream.Position = 0;
            _binReader = new BinaryReader(_memoryStream);
        }

        public Packet()
        {
            _memoryStream = new MemoryStream();
            _binWriter = new BinaryWriter(_memoryStream);
        }

        public Packet(byte pOpcode)
        {
            _memoryStream = new MemoryStream();
            _binWriter = new BinaryWriter(_memoryStream);
            WriteByte(pOpcode);
        }

        public void Dispose()
        {
            _memoryStream?.Dispose();
            _binReader?.Dispose();
            _binWriter?.Dispose();
        }

        public Packet(ServerMessages pMessage) : this((byte)pMessage) { }
        public Packet(ISClientMessages pMessage) : this((byte)pMessage) { }
        public Packet(ISServerMessages pMessage) : this((byte)pMessage) { }

        public byte[] ToArray()
        {
            return _memoryStream.ToArray();
        }

        public void GzipCompress(Packet packet) => GzipCompress(packet.MemoryStream);

        /// <summary>
        /// Compress the current buffer to a stream
        /// </summary>
        /// <param name="outputStream"></param>
        public void GzipCompress(Stream outputStream)
        {
            var pos = Position;
            Position = 0;
            using (var gzipStream = new GZipStream(outputStream, CompressionMode.Compress, true))
            {
                MemoryStream.CopyTo(gzipStream);
            }
            Position = pos;
        }

        public void DeflateCompress(Stream outputStream)
        {
            var pos = Position;
            Position = 0;
            using (var deflateStream = new DeflateStream(outputStream, CompressionMode.Compress, true))
            {
                MemoryStream.CopyTo(deflateStream);
            }
            Position = pos;
        }

        public int Length
        {
            get { return (int)_memoryStream.Length; }
        }

        public int Position
        {
            get { return (int)_memoryStream.Position; }
            set { _memoryStream.Position = value; }
        }

        public void Reset(int pPosition = 0)
        {
            _memoryStream.Position = pPosition;
        }

        public void Skip(int pAmount)
        {
            if (pAmount + _memoryStream.Position > Length)
                throw new Exception("!!! Cannot skip more bytes than there's inside the buffer!");
            _memoryStream.Position += pAmount;
        }

        public byte[] ReadLeftoverBytes()
        {
            return ReadBytes(Length - (int)_memoryStream.Position);
        }

        public override string ToString()
        {
            var sb = new StringBuilder(Length * 3);
            foreach (var b in ToArray())
            {
                sb.AppendFormat("{0:X2} ", b);
            }
            return sb.ToString();
        }

        public void WriteBytes(byte[] val) { _binWriter.Write(val); }

        public void WriteByte(byte val)
        {
            if (Length == 0)
                Opcode = val;
            _binWriter.Write(val);
        }
        public void WriteByteAsInt(int val) { _binWriter.Write(val); }
        public void WriteSByte(sbyte val) { _binWriter.Write(val); }
        public void WriteBool(bool val) { WriteByte(val == true ? (byte)1 : (byte)0); }
        public void WriteShort(short val) { _binWriter.Write(val); }
        public void WriteInt(int val) { _binWriter.Write(val); }
        public void WriteLong(long val) { _binWriter.Write(val); }
        public void WriteUShort(ushort val) { _binWriter.Write(val); }
        public void WriteUInt(uint val) { _binWriter.Write(val); }
        public void WriteULong(ulong val) { _binWriter.Write(val); }
        public void WriteDouble(double val) { _binWriter.Write(val); }
        public void WriteFloat(float val) { _binWriter.Write(val); }
        public void WriteString(string val) { WriteShort((short)val.Length); _binWriter.Write(val.ToCharArray()); }
        public void WriteString(string val, int maxlen) { var i = 0; for (; i < val.Length & i < maxlen; i++) _binWriter.Write(val[i]); for (; i < maxlen; i++) WriteByte(0); }
        public void WriteString13(string val) { WriteString(val, 13); }

        public void WriteHexString(string pInput)
        {
            pInput = pInput.Replace(" ", "");
            if (pInput.Length % 2 != 0) throw new Exception("Hex String is incorrect (size)");
            for (int i = 0; i < pInput.Length; i += 2)
            {
                WriteByte(byte.Parse(pInput.Substring(i, 2), System.Globalization.NumberStyles.HexNumber));
            }

        }

        public byte[] ReadBytes(int pLen) { return _binReader.ReadBytes(pLen); }
        public bool ReadBool() { return _binReader.ReadByte() != 0; }
        public byte ReadByte() { return _binReader.ReadByte(); }
        public sbyte ReadSByte() { return _binReader.ReadSByte(); }
        public short ReadShort() { return _binReader.ReadInt16(); }
        public int ReadInt() { return _binReader.ReadInt32(); }
        public long ReadLong() { return _binReader.ReadInt64(); }
        public ushort ReadUShort() { return _binReader.ReadUInt16(); }
        public uint ReadUInt() { return _binReader.ReadUInt32(); }
        public ulong ReadULong() { return _binReader.ReadUInt64(); }
        public double ReadDouble() { return _binReader.ReadDouble(); }
        public float ReadFloat() { return _binReader.ReadSingle(); }
        public string ReadString(short pLen = -1) { short len = pLen == -1 ? _binReader.ReadInt16() : pLen; return new string(_binReader.ReadChars(len)); }

        public void SetBytes(int pPosition, byte[] val) { int tmp = (int)_memoryStream.Position; Reset(pPosition); _binWriter.Write(val); Reset(tmp); }
        public void SetByte(int pPosition, byte val) { int tmp = (int)_memoryStream.Position; Reset(pPosition); _binWriter.Write(val); Reset(tmp); }
        public void SetSByte(int pPosition, sbyte val) { int tmp = (int)_memoryStream.Position; Reset(pPosition); _binWriter.Write(val); Reset(tmp); }
        public void SetBool(int pPosition, bool val) { int tmp = (int)_memoryStream.Position; Reset(pPosition); WriteByte(val == true ? (byte)1 : (byte)0); Reset(tmp); }
        public void SetShort(int pPosition, short val) { int tmp = (int)_memoryStream.Position; Reset(pPosition); _binWriter.Write(val); Reset(tmp); }
        public void SetInt(int pPosition, int val) { int tmp = (int)_memoryStream.Position; Reset(pPosition); _binWriter.Write(val); Reset(tmp); }
        public void SetLong(int pPosition, long val) { int tmp = (int)_memoryStream.Position; Reset(pPosition); _binWriter.Write(val); Reset(tmp); }
        public void SetUShort(int pPosition, ushort val) { int tmp = (int)_memoryStream.Position; Reset(pPosition); _binWriter.Write(val); Reset(tmp); }
        public void SetUInt(int pPosition, uint val) { int tmp = (int)_memoryStream.Position; Reset(pPosition); _binWriter.Write(val); Reset(tmp); }
        public void SetULong(int pPosition, ulong val) { int tmp = (int)_memoryStream.Position; Reset(pPosition); _binWriter.Write(val); Reset(tmp); }

    }
}
