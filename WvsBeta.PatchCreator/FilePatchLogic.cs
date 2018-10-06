using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace WvsBeta.PatchCreator
{
    interface PatchStep { }

    struct UseOriginalBlock : PatchStep
    {
        public int offset;
        public int amount;
    }

    struct UseNewBlock : PatchStep
    {
        public byte[] bytes;
    }

    class FilePatchLogic : IPatchedFile
    {
        private Queue<PatchStep> _steps = new Queue<PatchStep>();

        public string Filename { get; set; }
        public uint OldChecksum { get; set; }
        public uint NewChecksum { get; set; }
        public string OldFile { get; set; }
        public string NewFile { get; set; }

        public FilePatchLogic(string oldFile, string newFile)
        {
            OldFile = oldFile;
            NewFile = newFile;
            OldChecksum = CRC32.CalculateChecksumFile(OldFile);
            NewChecksum = CRC32.CalculateChecksumFile(NewFile);

            Filename = oldFile;
        }

        public void Init()
        {
        }

        public void Cleanup()
        {
        }

        private int lpOld = -1;
        private int lpNew = -1;
        private void WriteProgress(ref int lastProgress, int cur, int most)
        {
            var percentage = (int)(((long)cur * 100) / most);
            if (percentage == 0) percentage = 1;
            if (lastProgress == percentage) return;
            lastProgress = percentage;

            percentage /= 2;

            Console.Write('[');
            Console.Write(new string('X', percentage));
            Console.Write(new string('-', 50 - percentage));
            Console.Write("] {0}%", lastProgress);
            Console.WriteLine();

        }


        public void Run()
        {
            int consolePos = Console.CursorTop;

            WriteProgress(ref lpOld, 0, 100);
            WriteProgress(ref lpNew, 0, 100);
            var process = new Process();
            process.StartInfo.FileName = Path.Combine(Environment.CurrentDirectory, "jdiff.exe");
            process.StartInfo.Arguments = $"-b -lr \"{OldFile}\" \"{NewFile}\"";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.Start();

            var re = new Regex(@"\s*([0-9]+)\s*([0-9]+)\s*([A-Z]{3})\s*([0-9]+)\s*");

            var oldFileSize = (int)File.OpenRead(OldFile).Length;

            using (var newFS = File.OpenRead(NewFile))
            using (var sr = process.StandardOutput)
            {
                var newFileSize = (int)newFS.Length;

                while (true)
                {
                    if (sr.EndOfStream) break;
                    var line = sr.ReadLine();
                    var elements = re.Match(line);
                    var groups = elements.Groups;
                    var oldPos = int.Parse(groups[1].Value);
                    var newPos = int.Parse(groups[2].Value);
                    var mode = groups[3].Value;
                    var amount = int.Parse(groups[4].Value);

                    if (mode == "EQL")
                    {
                        _steps.Enqueue(new UseOriginalBlock
                        {
                            offset = oldPos,
                            amount = amount
                        });
                    }
                    else if (mode == "MOD" || mode == "INS")
                    {
                        byte[] tmp = new byte[amount];
                        newFS.Position = newPos;
                        newFS.Read(tmp, 0, amount);

                        _steps.Enqueue(new UseNewBlock
                        {
                            bytes = tmp
                        });
                    }
                    // Ignore deletes
                    else if (mode == "DEL") { }
                    // Backtraces are solved in the code (will be succeeded with an EQL, INS or whatever)
                    else if (mode == "BKT") { }
                    else
                    {
                        throw new Exception(mode);
                    }
                    Console.CursorTop = consolePos;
                    WriteProgress(ref lpOld, oldPos, oldFileSize);
                    WriteProgress(ref lpNew, newPos, newFileSize);
                }
            }
            Console.CursorTop = consolePos;
            WriteProgress(ref lpOld, 100, 100);
            WriteProgress(ref lpNew, 100, 100);
        }
        public void WriteToFile(BinaryWriter bw)
        {
            bw.Write((byte)1);
            bw.Write(OldChecksum);
            bw.Write(NewChecksum);

            Console.WriteLine("Patch steps calculated: {0}", _steps.Count);

            PatchStep ps;
            while (_steps.Count != 0)
            {
                ps = _steps.Dequeue();
                if (ps is UseOriginalBlock uob)
                {
                    bw.Write((uint)uob.amount);
                    bw.Write((uint)uob.offset);
                }
                else if (ps is UseNewBlock unb)
                {
                    uint val = (uint)unb.bytes.Length;
                    val |= 0x80000000;

                    byte v = unb.bytes[0];
                    // Not supported by NXPatcher
                    if (unb.bytes.Length > 1 && unb.bytes.All(x => x == v) && false)
                    {
                        // Repeated byte, 0xC0000000 (0x8 + 0x4)
                        val |= 0x40000000;
                        bw.Write(val);
                        bw.Write(v);
                    }
                    else
                    {
                        bw.Write(val);
                        bw.Write(unb.bytes);
                    }
                }
            }

            bw.Write((uint)0);
        }
    }
}
