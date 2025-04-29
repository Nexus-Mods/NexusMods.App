using System.Text;
using DynamicData.Kernel;
using NexusMods.Games.Larian.BaldursGate3.Utils.LsxXmlParsing;

namespace NexusMods.Games.Larian.BaldursGate3.Utils.PakParsing;

/// <summary>
/// Static class containing definitions for the Larian Package (LSPK, `.pak`) format.
/// </summary>
public static class LspkPackageFormat
{

    public struct PakMetaData
    {
        public LsxXmlFormat.MetaFileData MetaFileData;
        public Optional<ScriptExtenderConfigMetadata> ScriptExtenderConfigMetadata;
    }
    public struct ScriptExtenderConfigMetadata
    {
        public bool RequiresScriptExtender;
        public int SeRequiredVersion;
    }
    
#region Enums

    public enum PackageVersion
    {
        // V7 = 7, // D:OS 1
        // V9 = 9, // D:OS 1 EE
        // V10 = 10, // D:OS 2
        // V13 = 13, // D:OS 2 DE

        V15 = 15, // BG3 EA
        V16 = 16, // BG3 EA Patch4

        // V17 was skipped apparently
        V18 = 18, // BG3 Release
    };

    [Flags]
    public enum PackageFlags
    {
        /// <summary>
        /// Allow memory-mapped access to the files in this archive.
        /// </summary>
        AllowMemoryMapping = 0x02,

        /// <summary>
        /// All files are compressed into a single LZ4 stream
        /// </summary>
        Solid = 0x04,

        /// <summary>
        /// Archive contents should be preloaded on game startup.
        /// </summary>
        Preload = 0x08,
    };

    public enum CompressionFlags : byte
    {
        MethodNone = 0,
        MethodZlib = 1,
        MethodLZ4 = 2,
        MethodZstd = 3,
        FastCompress = 0x10,
        DefaultCompress = 0x20,
        MaxCompress = 0x40,
    };

    public enum CompressionMethod
    {
        None,
        Zlib,
        LZ4,
        Zstd
    };

#endregion // Enums

#region Header

    public class HeaderCommon
    {
        public const PackageVersion CurrentVersion = PackageVersion.V18;
        public const UInt32 SIGNATURE = 0x4B50534C;
        public const string SIGNATURE_STRING = "LSPK";

        public UInt32 Version;

        public UInt64 FileListOffset;

        // Size of file list; used for legacy (<= v10) packages only
        public UInt32 FileListSize;
        public UInt32 NumParts;
        public PackageFlags Flags;
        public Byte Priority;
        public required byte[] Md5;
    }

#region Internal Header versions

    internal struct LSPKHeader15
    {
        public UInt32 Version;
        public UInt64 FileListOffset;
        public UInt32 FileListSize;
        public Byte Flags;
        public Byte Priority;
        public byte[] Md5;

        public const int Md5Size = 16;

        public readonly HeaderCommon ToCommonHeader()
        {
            return new HeaderCommon
            {
                Version = Version,
                FileListOffset = FileListOffset,
                FileListSize = FileListSize,
                NumParts = 1,
                Flags = (PackageFlags)Flags,
                Priority = Priority,
                Md5 = Md5,
            };
        }
    }

    /// <summary>
    /// For both v16 and v18
    /// </summary>
    internal struct LSPKHeader16Or18
    {
        public UInt32 Version;
        public UInt64 FileListOffset;
        public UInt32 FileListSize;
        public Byte Flags;
        public Byte Priority;
        public byte[] Md5;
        public UInt16 NumParts;

        public const int Md5Size = 16;

        public readonly HeaderCommon ToCommonHeader()
        {
            return new HeaderCommon
            {
                Version = Version,
                FileListOffset = FileListOffset,
                FileListSize = FileListSize,
                NumParts = NumParts,
                Flags = (PackageFlags)Flags,
                Priority = Priority,
                Md5 = Md5,
            };
        }
    }

#endregion // Internal Header versions

#endregion // Header

#region FileEntryInfo

    public struct FileEntryInfoCommon
    {
        public string Name;
        public UInt32 Crc;
        public CompressionFlags Flags;
        public UInt64 OffsetInFile;
        public UInt64 SizeOnDisk;
        public UInt64 UncompressedSize;
    }

#region Internal FileEntry versions

    internal interface ILSPKFile
    {
        public FileEntryInfoCommon ToCommonFileEntry();
    }

    /// <summary>
    /// For v15 and v16
    /// </summary>
    internal struct FileEntry15Or16 : ILSPKFile
    {
        public byte[] Name;

        public UInt64 OffsetInFile;
        public UInt64 SizeOnDisk;
        public UInt64 UncompressedSize;
        public UInt32 ArchivePart;
        public UInt32 Flags;
        public UInt32 Crc;
        public UInt32 Unknown2;

        public const int Size = 296;

        public readonly FileEntryInfoCommon ToCommonFileEntry() => new FileEntryInfoCommon
        {
            Name = NullTerminatedBytesToString(Name),
            Crc = Crc,
            Flags = (CompressionFlags)Flags,
            OffsetInFile = OffsetInFile,
            SizeOnDisk = SizeOnDisk,
            UncompressedSize = UncompressedSize,
        };
    }

    internal struct FileEntry18 : ILSPKFile
    {
        public byte[] Name;

        public UInt32 OffsetInFile1;
        public UInt16 OffsetInFile2;
        public Byte ArchivePart;
        public Byte Flags;
        public UInt32 SizeOnDisk;
        public UInt32 UncompressedSize;

        public const int Size = 272;

        public readonly FileEntryInfoCommon ToCommonFileEntry() => new FileEntryInfoCommon
        {
            Name = Encoding.UTF8.GetString(Name).TrimEnd('\0'),
            Crc = 0,
            Flags = (CompressionFlags)Flags,
            OffsetInFile = OffsetInFile1 | (UInt64)OffsetInFile2 << 32,
            SizeOnDisk = SizeOnDisk,
            UncompressedSize = UncompressedSize,
        };
    }

#endregion // Internal FileEntry versions

#endregion // FileEntryInfo

#region Internal utility methods

    internal static int GetFileEntrySize(HeaderCommon header)
    {
        return header.Version switch
        {
            15 => FileEntry15Or16.Size,
            16 => FileEntry15Or16.Size,
            18 => FileEntry18.Size,
            _ => throw new InvalidOperationException($"Unsupported version {header.Version}"),
        };
    }

#endregion // Internal utility methods

#region Private methods

    private static String NullTerminatedBytesToString(byte[] b)
    {
        int len;
        for (len = 0; len < b.Length && b[len] != 0; len++)
        {
        }

        return Encoding.UTF8.GetString(b, 0, len);
    }

#endregion // Private methods
}

#region CompressionFlagsExtensions

public static class CompressionFlagExtensions
{
    public static LspkPackageFormat.CompressionMethod Method(this LspkPackageFormat.CompressionFlags f)
    {
        return (LspkPackageFormat.CompressionFlags)((byte)f & 0x0F) switch
        {
            LspkPackageFormat.CompressionFlags.MethodNone => LspkPackageFormat.CompressionMethod.None,
            LspkPackageFormat.CompressionFlags.MethodZlib => LspkPackageFormat.CompressionMethod.Zlib,
            LspkPackageFormat.CompressionFlags.MethodLZ4 => LspkPackageFormat.CompressionMethod.LZ4,
            LspkPackageFormat.CompressionFlags.MethodZstd => LspkPackageFormat.CompressionMethod.Zstd,
            _ => throw new NotSupportedException($"Unsupported compression method: {(byte)f & 0x0F}")
        };
    }
}

#endregion // CompressionFlagsExtensions
