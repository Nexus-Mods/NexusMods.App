using System.Text;
using K4os.Compression.LZ4;
using Newtonsoft.Json;

namespace NexusMods.Games.Larian.BaldursGate3.Utils.PakParsing;

/// <summary>
/// Class to parse and extract files and data from a bg3 .pak file.
/// Credits to @insomnious for reverse engineering the format and implementing the parser. 
/// </summary>
public class PakFileLoader
{
    private const string MAGIC_BYTES = "LSPK";
    private const int FILE_ENTRY_SIZE = 272;

#region Public DataTypes

    /// <summary>
    /// Pak file header data
    /// </summary>
    public struct Header
    {
        public uint Version;
        public ulong FileListOffset;
        public uint FileListSize;
        public byte Flags;
        public byte Priority;
        public byte[] Md5;
        public ushort NumParts;
    }

    /// <summary>
    /// Data of a file entry in the list of files contained in the pak file
    /// Version 18
    /// </summary>
    public struct FileEntry18
    {
        public string Name;
        public uint OffsetInFile1;
        public ushort OffsetInFile2;
        public byte ArchivePart;
        public byte Flags;
        public uint SizeOnDisk;
        public uint UncompressedSize;
    }

#endregion

#region Public Methods

    public void LoadFromFile(string filePath)
    {
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using (var br = new BinaryReader(fs))
        {
            Load(br);
        }
    }

    public void LoadFromByteArray(byte[] fileData)
    {
        using var ms = new MemoryStream(fileData);
        using (var br = new BinaryReader(ms))
        {
            Load(br);
        }
    }
    
    public void LoadFromStream(Stream stream)
    {
        using (var br = new BinaryReader(stream))
        {
            Load(br);
        }
    }

#endregion

#region Private Methods

    private void Load(BinaryReader br)
    {
        var magic = br.ReadBytes(4);

        if (Encoding.UTF8.GetString(magic) != MAGIC_BYTES)
        {
            throw new Exception($"Not a valid BG3 PAK. Magic bytes ({MAGIC_BYTES}) not found.");
        }

        var data = new Header
        {
            Version = br.ReadUInt32(),
            FileListOffset = br.ReadUInt64(),
            FileListSize = br.ReadUInt32(),
            Flags = br.ReadByte(),
            Priority = br.ReadByte(),
            Md5 = br.ReadBytes(16),
            NumParts = br.ReadUInt16(),
        };

        // display header
        Console.WriteLine(JsonConvert.SerializeObject(data, Formatting.Indented));

        ReadCompressedFileList(br, (int)data.FileListOffset);
    }

    private void ReadCompressedFileList(BinaryReader br, int offset)
    {
        br.BaseStream.Seek(offset, SeekOrigin.Begin);

        var numOfFiles = br.ReadInt32();
        var compressedSize = br.ReadInt32();

        Console.WriteLine($"Number of files: {numOfFiles}");
        Console.WriteLine($"Compressed size: {compressedSize}");

        var decompressedSize = numOfFiles * FILE_ENTRY_SIZE;
        var compressed = br.ReadBytes(compressedSize);

        var decompressed = new byte[decompressedSize];
        var decodedBytes = LZ4Codec.Decode(compressed,
            0,
            compressed.Length,
            decompressed,
            0,
            decompressed.Length
        );

        Console.WriteLine($"DecodedBytes {decodedBytes}");

        if (decodedBytes == 0)
        {
            throw new InvalidOperationException("Decompression failed.");
        }

        //Array.Resize(ref decompressed, decodedBytes);
        Console.WriteLine("Decompression successful.");

        // write temp bytes so we can see what we're working with
        File.WriteAllBytes(@"C:\Work\bg3pak\paks\temp.bin", decompressed);

        // new mem stream from decompress bytes
        using var ms = new MemoryStream(decompressed);
        using var msr = new BinaryReader(ms);

        // built up list of file entries
        var entries = new List<FileEntry18>();

        for (var i = 0; i < numOfFiles; i++)
        {
            var entry = new FileEntry18
            {
                Name = Encoding.UTF8.GetString(msr.ReadBytes(256)).TrimEnd('\0'),
                OffsetInFile1 = msr.ReadUInt32(),
                OffsetInFile2 = msr.ReadUInt16(),
                ArchivePart = msr.ReadByte(),
                Flags = msr.ReadByte(),
                SizeOnDisk = msr.ReadUInt32(),
                UncompressedSize = msr.ReadUInt32(),
            };

            entries.Add(entry);
        }

        // look through file entries for meta.lsx
        var metaLsx = entries.FirstOrDefault(e => e.Name.Contains("meta.lsx"));

        // if we have something, then read the data
        if (metaLsx.Name != null)
        {
            Console.WriteLine(JsonConvert.SerializeObject(metaLsx, Formatting.Indented));

            var metaLsxData = ReadFileEntryData(br,
                metaLsx,
                (int)metaLsx.OffsetInFile1,
                (int)metaLsx.SizeOnDisk
            );

            var metaLsxFilePath = @"C:\Work\bg3pak\paks\meta.lsx";
            File.WriteAllBytes(metaLsxFilePath, metaLsxData);
        }
        else
        {
            Console.WriteLine("meta.lsx not found.");
        }
    }

    private byte[] ReadFileEntryData(BinaryReader br, FileEntry18 fileMeta, int offset, int size)
    {
        br.BaseStream.Seek(offset, SeekOrigin.Begin);

        var data = br.ReadBytes(size);
        var decompressed = new byte[fileMeta.UncompressedSize];

        var decodedBytes = LZ4Codec.Decode(data,
            0,
            data.Length,
            decompressed,
            0,
            decompressed.Length
        );

        return decompressed;
    }

#endregion
}
