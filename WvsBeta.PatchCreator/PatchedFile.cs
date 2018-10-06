using System.IO;

namespace WvsBeta.PatchCreator
{
    interface IPatchedFile
    {
        string Filename { get; }
        void WriteToFile(BinaryWriter bw);
        void Cleanup();
    }

    class RemovedFile : IPatchedFile
    {
        public string Filename { get; set; }
        public void WriteToFile(BinaryWriter bw)
        {
            bw.Write((byte)2);
        }

        public void Cleanup() { }
    }

    class AddedFile : IPatchedFile
    {
        public string Filename { get; set; }
        public void WriteToFile(BinaryWriter bw)
        {
            bw.Write((byte)0);

            using (var file = File.OpenRead(Filename))
            {
                bw.Write((int)file.Length);
                bw.Write(CRC32.CalculateChecksumStream(file));
                file.Position = 0;
                file.CopyTo(bw.BaseStream);
            }
        }
        public void Cleanup() { }
    }

    class AddedDirectory : IPatchedFile
    {
        public string Filename { get; set; }
        public void WriteToFile(BinaryWriter bw)
        {
            // Filename should end with a \\
            bw.Write((byte)0);
        }
        public void Cleanup() { }
    }
}
