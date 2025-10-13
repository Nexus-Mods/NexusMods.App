using NexusMods.Paths;
#pragma warning disable CS1591 // missing XML documentation
// ReSharper disable All
namespace NexusMods.Abstractions.FileExtractor {

    public enum FileType
    {            /// <summary>
            /// Windows Batch File
            /// </summary>
             BAT,
                /// <summary>
                /// Bethesda Tar
                /// </summary>
                 BTAR,
                        /// <summary>
            /// Windows Command File
            /// </summary>
             CMD,
            /// <summary>
            /// OSX Command File
            /// </summary>
             COMMAND,
                /// <summary>
                /// Creation Engine Plugin
                /// </summary>
                 CreationEnginePlugin,
                            /// <summary>
                /// Cyberpunk Appearance Preset
                /// </summary>
                 Cyberpunk2077AppearancePreset,
                        /// <summary>
            /// Ini Configuration File
            /// </summary>
             INI,
            /// <summary>
            /// JSON File
            /// </summary>
             JSON,
                /// <summary>
                /// Test files
                /// </summary>
                 JustTest,
                            /// <summary>
                /// Morrowind BSA
                /// </summary>
                 MorrowindBSA,
                            /// <summary>
                /// NetImmerse File Format
                /// </summary>
                 NIF,
                        /// <summary>
            /// Unix Shell Script
            /// </summary>
             SH,
                /// <summary>
                /// TES4 Plugin
                /// </summary>
                 TES4,
                        /// <summary>
            /// Text File
            /// </summary>
             TXT,
            /// <summary>
            /// XML File
            /// </summary>
             XML,
                /// <summary>
                /// 7-Zip compressed file
                /// </summary>
                _7Z,
                            /// <summary>
                /// FO4 BSA
                /// </summary>
                BA2,
                            /// <summary>
                /// TES 4-5 and FO 3 BSA
                /// </summary>
                BSA,
                            /// <summary>
                /// DDS
                /// </summary>
                DDS,
                            /// <summary>
                /// PDF file
                /// </summary>
                FDF,
                            /// <summary>
                /// GZIP archive file
                /// </summary>
                GZ,
                            /// <summary>
                /// PDF file
                /// </summary>
                PDF,
                            /// <summary>
                /// Relaxed RAR format
                /// </summary>
                RAR,
                            /// <summary>
                /// RAR5 or newer
                /// </summary>
                RAR_NEW,
                            /// <summary>
                /// RAR4 or older
                /// </summary>
                RAR_OLD,
                            /// <summary>
                /// Zip
                /// </summary>
                ZIP,
                }

    public static class Definitions {


    public static readonly (FileType, byte[])[] Signatures = {
            // 7-Zip compressed file
        (FileType._7Z, new byte[] {0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C}),

                // Zip
        (FileType.ZIP, new byte[] {0x50, 0x4B, 0x03, 0x04}),

                // Zip
        (FileType.ZIP, new byte[] {0x50, 0x4B, 0x05, 0x06}),

                // Zip
        (FileType.ZIP, new byte[] {0x50, 0x4B, 0x07, 0x08}),

                // Relaxed RAR format
        (FileType.RAR, new byte[] {0x52, 0x61, 0x72, 0x21}),

                // RAR5 or newer
        (FileType.RAR_NEW, new byte[] {0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x01, 0x00}),

                // RAR4 or older
        (FileType.RAR_OLD, new byte[] {0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x00}),

                // Morrowind BSA
        (FileType. MorrowindBSA, new byte[] {0x00, 0x01, 0x00, 0x00}),

                // TES 4-5 and FO 3 BSA
        (FileType.BSA, new byte[] {0x42, 0x53, 0x41, 0x00}),

                // FO4 BSA
        (FileType.BA2, new byte[] {0x42, 0x54, 0x44, 0x58}),

                // DDS
        (FileType.DDS, new byte[] {0x44, 0x44, 0x53, 0x20}),

                // Bethesda Tar
        (FileType. BTAR, new byte[] {0x42, 0x54, 0x41, 0x52}),

                // GZIP archive file
        (FileType.GZ, new byte[] {0x1F, 0x8B, 0x08}),

                // NetImmerse File Format
        (FileType. NIF, new byte[] {0x4e, 0x65, 0x74, 0x49, 0x6d, 0x6d, 0x65, 0x72, 0x73, 0x65, 0x20, 0x46, 0x69, 0x6c, 0x65, 0x20, 0x46, 0x6f, 0x72, 0x6d, 0x61, 0x74}),

                // Gamebryo File Format
        (FileType. NIF, new byte[] {0x47, 0x61, 0x6d, 0x65, 0x62, 0x72, 0x79, 0x6f, 0x20, 0x46, 0x69, 0x6c, 0x65, 0x20, 0x46, 0x6f, 0x72, 0x6d, 0x61, 0x74}),

                // Creation Engine Plugin
        (FileType. CreationEnginePlugin, new byte[] {0x54, 0x45, 0x53, 0x34}),

                // TES4 Plugin
        (FileType. TES4, new byte[] {0x54, 0x45, 0x53, 0x34}),

                // PDF file
        (FileType.PDF, new byte[] {0x25, 0x50, 0x44, 0x46}),

                // PDF file
        (FileType.FDF, new byte[] {0x25, 0x50, 0x44, 0x46}),

                // Cyberpunk Appearance Preset
        (FileType. Cyberpunk2077AppearancePreset, new byte[] {0x4c, 0x6f, 0x63, 0x4b, 0x65, 0x79, 0x23}),

                // Test files
        (FileType. JustTest, new byte[] {0x4A, 0x55, 0x53, 0x54, 0x54, 0x45, 0x53, 0x54}),

        
    };

    public static readonly (FileType, Extension)[] Extensions = {
                // Ini Configuration File
        (FileType.INI, new Extension(".ini")),

                // Text File
        (FileType.TXT, new Extension(".txt")),

                // JSON File
        (FileType.JSON, new Extension(".json")),

                // XML File
        (FileType.XML, new Extension(".xml")),

                // Unix Shell Script
        (FileType.SH, new Extension(".sh")),

                // Windows Batch File
        (FileType.BAT, new Extension(".bat")),

                // Windows Command File
        (FileType.CMD, new Extension(".cmd")),

                // OSX Command File
        (FileType.COMMAND, new Extension(".command")),

        
    };

}}
