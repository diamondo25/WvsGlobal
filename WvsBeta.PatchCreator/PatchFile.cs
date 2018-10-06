using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Org.BouncyCastle.Utilities.Zlib;

namespace WvsBeta.PatchCreator
{
    static class PatchFile
    {
        public static void BuildPatchfile(string newDir, string oldDir, string outputFile, params IPatchedFile[] files)
        {
            using (var fs = File.Open(outputFile, FileMode.Create))
            using (var totalFileInMem = new MemoryStream())
            using (var bw = new BinaryWriter(totalFileInMem))
            {
                bw.Write(Encoding.ASCII.GetBytes("WzPatch\x1A")); // 8
                bw.Write((int)2); // Version?, 12
                var checksumOffset = bw.BaseStream.Position;
                bw.Write((int)0); // Checksum, 16

                using (var compressedSubBlob = new MemoryStream())
                using (var ds = new ZOutputStream(compressedSubBlob, 6))
                using (var bw_ms = new BinaryWriter(ds))
                //using (var zs = new GZipStream(compressedSubBlob, CompressionLevel.Optimal))
                //using (var bw_ms = new BinaryWriter(zs))
                {
                    foreach (var filePatchLogic in files)
                    {
                        var fn = filePatchLogic.Filename.Replace(newDir, "").Replace(oldDir, "").Substring(1);

                        bw_ms.Write(fn.ToCharArray());

                        Console.WriteLine("Writing patch step of {0} ({1})", fn, filePatchLogic);
                        filePatchLogic.WriteToFile(bw_ms);
                    }

                    bw_ms.Flush();
                    //zs.Flush();
                    ds.Finish();

                    Console.WriteLine("Compressed data, flushing to main stream...");
                    compressedSubBlob.WriteTo(totalFileInMem);
                }

                Console.WriteLine("All done, just working on CRC...");

                bw.BaseStream.Position = checksumOffset + 4;
                var checksum = CRC32.CalculateChecksumStream(totalFileInMem);
                bw.BaseStream.Position = checksumOffset;
                bw.Write(checksum);

                Console.WriteLine("Flushing to file...");
                // Copy it over to the file
                totalFileInMem.WriteTo(fs);
            }
        }


    }
}
