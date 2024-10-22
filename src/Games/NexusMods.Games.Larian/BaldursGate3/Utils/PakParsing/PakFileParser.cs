using System.IO.Compression;
using System.Text;
using DynamicData.Kernel;
using K4os.Compression.LZ4;
using NexusMods.Games.Larian.BaldursGate3.Utils.LsxXmlParsing;
using ZstdSharp;

namespace NexusMods.Games.Larian.BaldursGate3.Utils.PakParsing;

/// <summary>
/// Class to parse and extract files and data from a bg3 .pak file.
/// Credits to @insomnious for reverse engineering the format and implementing the parser. 
/// </summary>
public static class PakFileParser
{

#region Public Methods

    /// <summary>
    /// Parses a bg3 `.pak` file stream and extracts the metadata from the packed `meta.lsx` file.
    /// </summary>
    /// <exception cref="InvalidDataException">In case of errors during parsing</exception>
    public static LsxXmlFormat.MetaFileData ParsePakMeta(Stream pakFileStream)
    {
        using var br = new BinaryReader(pakFileStream);
        var headerData = ParseHeaderInternal(br);
        var fileList = ParseFileListInternal(br, (int)headerData.FileListOffset, headerData);

        var fileEntryInfo = fileList.FirstOrOptional(f => f.Name.Contains("meta.lsx"));
        if (!fileEntryInfo.HasValue)
        {
            throw new InvalidDataException($"Unable to find `meta.lsx` file in pak archive");
        }

        var metaStream = ReadFileEntryData(br, fileEntryInfo.Value);
        return MetaLsxParser.ParseMetaFile(metaStream);
    }

#endregion

#region Private Methods

    private static void Load(BinaryReader br)
    {
        var headerData = ParseHeaderInternal(br);

        var fileList = ParseFileListInternal(br, (int)headerData.FileListOffset, headerData);
    }

    private static LspkPackageFormat.HeaderCommon ParseHeaderInternal(BinaryReader br)
    {
        var magic = br.ReadBytes(4);
        var signature = Encoding.UTF8.GetString(magic);

        if (signature != LspkPackageFormat.HeaderCommon.SIGNATURE_STRING)
        {
            throw new InvalidDataException($"Not a valid BG3 PAK. Magic signature `{signature}` does not match expected signature `{LspkPackageFormat.HeaderCommon.SIGNATURE_STRING}`");
        }

        var version = br.ReadUInt32();

        switch (version)
        {
            case 15:
                return new LspkPackageFormat.LSPKHeader15()
                {
                    Version = version,
                    FileListOffset = br.ReadUInt64(),
                    FileListSize = br.ReadUInt32(),
                    Flags = br.ReadByte(),
                    Priority = br.ReadByte(),
                    Md5 = br.ReadBytes(16),
                }.ToCommonHeader();
            case 16:
            case 18:
                // same as 16 header
                return new LspkPackageFormat.LSPKHeader16Or18()
                {
                    Version = version,
                    FileListOffset = br.ReadUInt64(),
                    FileListSize = br.ReadUInt32(),
                    Flags = br.ReadByte(),
                    Priority = br.ReadByte(),
                    Md5 = br.ReadBytes(16),
                    NumParts = br.ReadUInt16(),
                }.ToCommonHeader();
            default:
                throw new InvalidDataException($"Unrecognized Pak version: v{version}");
        }
    }

    private static List<LspkPackageFormat.FileEntryInfoCommon> ParseFileListInternal(BinaryReader br, int offset, LspkPackageFormat.HeaderCommon header)
    {
        br.BaseStream.Seek(offset, SeekOrigin.Begin);

        var numOfFiles = br.ReadInt32();

        var compressedSize = br.ReadInt32();

        var compressedBytes = br.ReadBytes(compressedSize);

        var decompressedBytes = new byte[numOfFiles * LspkPackageFormat.GetFileEntrySize(header)];

        // Assumption that we always have LZ4 for v15-18 (same as LSLib) but could be wrong
        var numDecodedBytes = LZ4Codec.Decode(
            compressedBytes,
            0,
            compressedBytes.Length,
            decompressedBytes,
            0,
            decompressedBytes.Length
        );

        if (numDecodedBytes != decompressedBytes.Length)
        {
            throw new InvalidDataException($"Decompression failed: decompressed size {decompressedBytes.Length} does not match expected size {numDecodedBytes}");
        }

        // new mem stream from decompress bytes
        using var ms = new MemoryStream(decompressedBytes);
        using var msr = new BinaryReader(ms);

        // built up list of file entries
        var entries = new List<LspkPackageFormat.FileEntryInfoCommon>(numOfFiles);

        msr.BaseStream.Seek(0, SeekOrigin.Begin);

        for (var i = 0; i < numOfFiles; i++)
        {
            var entry = ParseFileEntryInternal(msr, (int)header.Version);

            entries.Add(entry);
        }

        return entries;
    }

    private static LspkPackageFormat.FileEntryInfoCommon ParseFileEntryInternal(BinaryReader br, int version)
    {
        switch (version)
        {
            case 15:
            case 16: // same as 15
            {
                return new LspkPackageFormat.FileEntry15Or16
                {
                    Name = br.ReadBytes(256),
                    OffsetInFile = br.ReadUInt64(),
                    SizeOnDisk = br.ReadUInt64(),
                    UncompressedSize = br.ReadUInt64(),
                    ArchivePart = br.ReadUInt32(),
                    Flags = br.ReadUInt32(),
                    Crc = br.ReadUInt32(),
                    Unknown2 = br.ReadUInt32()
                }.ToCommonFileEntry();
            }
            case 18:
            {
                return new LspkPackageFormat.FileEntry18
                {
                    Name = br.ReadBytes(256),
                    OffsetInFile1 = br.ReadUInt32(),
                    OffsetInFile2 = br.ReadUInt16(),
                    ArchivePart = br.ReadByte(),
                    Flags = br.ReadByte(),
                    SizeOnDisk = br.ReadUInt32(),
                    UncompressedSize = br.ReadUInt32()
                }.ToCommonFileEntry();
            }
            default:
                throw new InvalidDataException($"Unrecognized Pak version: v{version}");
        }
    }
    
    private static Stream ReadFileEntryData(BinaryReader br, LspkPackageFormat.FileEntryInfoCommon fileMeta)
    {
        br.BaseStream.Seek((long)fileMeta.OffsetInFile, SeekOrigin.Begin);

        var rawData = br.ReadBytes((int)fileMeta.SizeOnDisk);

        return fileMeta.Flags.Method() switch
        {
            LspkPackageFormat.CompressionMethod.None => new MemoryStream(rawData),
            LspkPackageFormat.CompressionMethod.LZ4 => DecompressLz4(fileMeta, rawData),
            LspkPackageFormat.CompressionMethod.Zlib => DecompressZlib(fileMeta, rawData),
            LspkPackageFormat.CompressionMethod.Zstd => DecompressZstd(fileMeta, rawData),
            _ => throw new InvalidDataException($"Unsupported compression method {fileMeta.Flags.Method()} for file {fileMeta.Name}")
        };

        Stream DecompressLz4(LspkPackageFormat.FileEntryInfoCommon fileEntryInfoCommon, byte[] bytes)
        {
            var decompressedBytes = new byte[fileEntryInfoCommon.UncompressedSize];
        
            var decodedSize = LZ4Codec.Decode(bytes, 0, bytes.Length, decompressedBytes, 0, decompressedBytes.Length);
            if (decodedSize != decompressedBytes.Length)
            {
                throw new InvalidDataException($"Failed to extract {fileEntryInfoCommon.Name} from Pak archive: decompressed size {decodedSize} does not match expected size {fileEntryInfoCommon.UncompressedSize}");
            }
            
            return new MemoryStream(decompressedBytes);
        }
        
        Stream DecompressZlib(LspkPackageFormat.FileEntryInfoCommon fileEntryInfoCommon, byte[] bytes)
        {
            using var ms = new MemoryStream(bytes);
            using var ds = new ZLibStream(ms, CompressionMode.Decompress);
            var decompressedBytes = new byte[fileEntryInfoCommon.UncompressedSize];
            var read = ds.Read(decompressedBytes, 0, decompressedBytes.Length);
            if (read != decompressedBytes.Length)
            {
                throw new InvalidDataException($"Failed to extract {fileEntryInfoCommon.Name} from Pak archive: decompressed size {read} does not match expected size {fileEntryInfoCommon.UncompressedSize}");
            }
            
            return new MemoryStream(decompressedBytes);
        }
        
        Stream DecompressZstd(LspkPackageFormat.FileEntryInfoCommon fileEntryInfoCommon, byte[] bytes)
        {
            using var ms = new MemoryStream(bytes);
            using var ds = new DecompressionStream(ms);
            var decompressedBytes = new byte[fileEntryInfoCommon.UncompressedSize];
            var read = ds.Read(decompressedBytes, 0, decompressedBytes.Length);
            if (read != decompressedBytes.Length)
            {
                throw new InvalidDataException($"Failed to extract {fileEntryInfoCommon.Name} from Pak archive: decompressed size {read} does not match expected size {fileEntryInfoCommon.UncompressedSize}");
            }
            
            return new MemoryStream(decompressedBytes);
        }
    }
    


#endregion
}
