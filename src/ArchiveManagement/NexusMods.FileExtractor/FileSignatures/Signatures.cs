
namespace Wabbajack.Common.FileSignatures {

    public enum FileType {        BTAR,
           CreationEnginePlugin,
           MorrowindBSA,
           NIF,
          _7Z,
          BA2,
          BSA,
          DDS,
          GZ,
          RAR,
          RAR_NEW,
          RAR_OLD,
          ZIP,
        }

    public static class Definitions {


    public static (FileType, byte[])[] Signatures = {
            // Morrowind BSA
        (FileType. MorrowindBSA, new byte[] {0x00, 0x01, 0x00, 0x00}),

                // TES 4-5 and FO 3 BSA
        (FileType.BSA, new byte[] {0x42, 0x53, 0x41, 0x00}),

                // FO4 BSA
        (FileType.BA2, new byte[] {0x42, 0x54, 0x44, 0x58}),

                // Relaxed RAR format
        (FileType.RAR, new byte[] {0x52, 0x61, 0x72, 0x21}),

                // RAR5 or newer
        (FileType.RAR_NEW, new byte[] {0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x01, 0x00}),

                // RAR4 or older
        (FileType.RAR_OLD, new byte[] {0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x00}),

                // DDS
        (FileType.DDS, new byte[] {0x44, 0x44, 0x53, 0x20}),

                // Bethesda Tar
        (FileType. BTAR, new byte[] {0x42, 0x54, 0x41, 0x52}),

                // GZIP archive file
        (FileType.GZ, new byte[] {0x1F, 0x8B, 0x08}),

                // 7-Zip compressed file
        (FileType._7Z, new byte[] {0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C}),

                // Zip
        (FileType.ZIP, new byte[] {0x50, 0x4B, 0x03, 0x04}),

                // Zip
        (FileType.ZIP, new byte[] {0x50, 0x4B, 0x05, 0x06}),

                // Zip
        (FileType.ZIP, new byte[] {0x50, 0x4B, 0x07, 0x08}),

                // NetImmerse File Format
        (FileType. NIF, new byte[] {0x4e, 0x65, 0x74, 0x49, 0x6d, 0x6d, 0x65, 0x72, 0x73, 0x65, 0x20, 0x46, 0x69, 0x6c, 0x65, 0x20, 0x46, 0x6f, 0x72, 0x6d, 0x61, 0x74}),

                // Gamebryo File Format
        (FileType. NIF, new byte[] {0x47, 0x61, 0x6d, 0x65, 0x62, 0x72, 0x79, 0x6f, 0x20, 0x46, 0x69, 0x6c, 0x65, 0x20, 0x46, 0x6f, 0x72, 0x6d, 0x61, 0x74}),

                // Creation Engine Plugin
        (FileType. CreationEnginePlugin, new byte[] {0x54, 0x45, 0x53, 0x34}),

        
    };}}