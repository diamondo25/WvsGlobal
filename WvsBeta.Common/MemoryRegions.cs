using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using log4net;

namespace WvsBeta.Common
{
    public struct MemoryRegion
    {
        public uint Address { get; }
        public byte[] Data { get; }

        public int Length => Data.Length;

        public MemoryRegion(uint address, byte[] data)
        {
            Address = address;
            Data = data;
        }
    }

    public class MemoryRegions
    {
        private static ILog log = LogManager.GetLogger("MemoryRegions");

        private static MemoryRegions _instance;

        public static MemoryRegions Instance => _instance ?? Init();

        public static MemoryRegions Init()
        {
            if (_instance != null) return _instance;
            _instance = new MemoryRegions();
            _instance.LoadRegions();
            return _instance;
        }

        public List<MemoryRegion> Regions = new List<MemoryRegion>();

        public int MaxRandomMemoryOffset { get; private set; } = 0;

        public void LoadRegions()
        {
            try
            {
                if (!File.Exists("MapleStory.exe"))
                {
                    throw new Exception("Not loading any regions; cannot find MapleStory.exe");

                }

                if (!File.Exists("MemoryRegions.tsv"))
                {
                    throw new Exception("Unable to load MemoryRegions.tsv");
                }

                using (var mapleFileStream = File.Open("MapleStory.exe", FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var mapleReader = new BinaryReader(mapleFileStream))
                using (var configReader = new StreamReader(File.OpenRead("MemoryRegions.tsv"), Encoding.ASCII))
                {
                    List<Tuple<string, uint, uint>> fileRegions = new List<Tuple<string, uint, uint>>();

                    // Skip to the file regions
                    while (true)
                    {
                        var line = ReadNextLine(configReader);
                        if (line == "START_FILE_REGIONS")
                        {
                            break;
                        }
                    }
                    while (true)
                    {
                        var line = ReadNextLine(configReader);
                        if (line == "START_MEMORY_REGIONS")
                        {
                            break;
                        }
                        var parsedLine = ParseFileRegionLine(line);
                        if (parsedLine != null)
                            fileRegions.Add(parsedLine);
                    }

                    Func<Tuple<uint, int>, byte[]> addressToDataFunc = (tuple) =>
                    {
                        var address = tuple.Item1;
                        var size = tuple.Item2;

                        uint lastStartAddress = 0;
                        uint lastOffset = 0;
                        foreach (var region in fileRegions)
                        {
                            // Find the region this address is in.
                            if (region.Item2 < address && region.Item2 > lastStartAddress)
                            {
                                lastStartAddress = region.Item2;
                                lastOffset = region.Item3;
                            }
                        }

                        mapleFileStream.Seek((address - lastStartAddress) + lastOffset, SeekOrigin.Begin);
                        return mapleReader.ReadBytes(size);
                    };

                    Regions.Clear();

                    while (true)
                    {
                        var line = ReadNextLine(configReader);
                        if (line == null)
                        {
                            break;
                        }
                        var parsedLine = ParseMemoryRegionLine(line);
                        if (parsedLine != null)
                        {
                            Regions.Add(new MemoryRegion(parsedLine.Item1, addressToDataFunc(parsedLine)));
                        }
                    }

                    int minimumSize = Regions.Select(x => x.Length).Min();
                    MaxRandomMemoryOffset = Math.Max(0, minimumSize - 1); // Make sure we do not go negative
                }
            }
            catch (Exception ex)
            {
                log.Error("Exception while loading MemoryRegions:", ex);
            }
        }

        private Tuple<string, uint, uint> ParseFileRegionLine(StreamReader reader)
        {
            string line = null;
            while (!reader.EndOfStream && line == null)
            {
                var tmp = ReadNextLine(reader);
                if (tmp.Count(x => x == '\t') < 2) continue;
                line = tmp;
            }
            return ParseFileRegionLine(line);
        }
        private Tuple<string, uint, uint> ParseFileRegionLine(string line)
        {
            if (line == null) return null;

            var elements = line.Split('\t');
            if (elements.Length < 3) return null;

            var name = elements[0];
            var startAddress = uint.Parse(elements[1], NumberStyles.HexNumber);
            var fileOffset = uint.Parse(elements[2], NumberStyles.HexNumber);
            return new Tuple<string, uint, uint>(name, startAddress, fileOffset);
        }

        private Tuple<uint, int> ParseMemoryRegionLine(StreamReader reader)
        {
            string line = null;
            while (!reader.EndOfStream && line == null)
            {
                var tmp = ReadNextLine(reader);
                if (tmp.Count(x => x == '\t') < 2) continue;
                line = tmp;
            }
            return ParseMemoryRegionLine(line);
        }

        private Tuple<uint, int> ParseMemoryRegionLine(string line)
        {
            if (line == null) return null;

            var elements = line.Split('\t');
            if (elements.Length < 2) return null;

            var address = uint.Parse(elements[0], NumberStyles.HexNumber);
            var length = int.Parse(elements[1], NumberStyles.Integer);
            return new Tuple<uint, int>(address, length);
        }

        private string ReadNextLine(StreamReader reader)
        {
            if (reader.EndOfStream) return null;
            string line = null;
            while (!reader.EndOfStream && line == null)
            {
                var tmp = reader.ReadLine();
                if (tmp.Length == 0 || tmp[0] == '#')
                {
                    continue;
                }

                line = tmp;
            }

            return line;
        }
    }
}
