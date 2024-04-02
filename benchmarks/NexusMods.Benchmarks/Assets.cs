using System.IO.Compression;
using System.Text;
using NexusMods.Paths;

namespace NexusMods.Benchmarks;

public class Assets
{
    public static string ProgramFolder => FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory).ToString();
    public static string AssetsFolder => Path.Combine(ProgramFolder, "Assets");

    public static string LoadoutsFolder => Path.Combine(AssetsFolder, "Loadouts");

    // ReSharper disable once InconsistentNaming
    public static string Sample7zFile => Path.Combine(AssetsFolder, "data_7zip_lzma2.7z");
    public static string SampleZipFile => Path.Combine(AssetsFolder, "data_zip_lzma.zip");

    /// <summary>
    ///     Assets related to fake/mocked loadouts.
    /// </summary>
    public static class Loadouts
    {
        public static string FileListsFolder => Path.Combine(LoadoutsFolder, "FileLists");

        /// <summary>
        ///     File lists used for simulating individual mods.
        /// </summary>
        public static class FileLists
        {
            /// <summary>
            ///     Interesting NPCs, Loose Files. 59447 paths.
            /// </summary>
            public static string NPC3DFileList => Path.Combine(FileListsFolder, "3dnpc-allfiles-59447paths.br");

            /// <summary>
            ///     Files that form the base set of Stardew Valley. 3514 paths.
            /// </summary>
            public static string StardewValleyFileList => Path.Combine(FileListsFolder, "stardewvalley-1.6-allfiles-3514paths.br");

            /// <summary>
            ///     Files that form the base set of Skyrim SE. 47 paths.
            /// </summary>
            public static string SkyrimFileList => Path.Combine(FileListsFolder, "skyrimse-47paths.br");

            /// <summary>
            ///     Retrieves the path of the file list by file name.
            /// </summary>
            public static string GetFileListPathByFileName(string fileName) => Path.Combine(FileListsFolder, fileName);

            /// <summary>
            ///     Retrieves the contents of a file list
            /// </summary>
            public static string[] GetFileList(string file)
            {
                // Note: We don't handle exceptions. If the file doesn't exist, we want the benchmark to fail.
                // Read the compressed file into a byte array
                var compressedData = File.ReadAllBytes(file);

                // Create a memory stream to hold the compressed data
                using var compressedStream = new MemoryStream(compressedData);
                using var decompressionStream = new BrotliStream(compressedStream, CompressionMode.Decompress);
                using var decompressedStream = new MemoryStream();

                // Decompress the data
                decompressionStream.CopyTo(decompressedStream);

                // Reset the position of the decompressed stream
                decompressedStream.Position = 0;

                // Read the decompressed data into a string
                using var reader = new StreamReader(decompressedStream, Encoding.UTF8);
                var decompressedText = reader.ReadToEnd();

                // Split the decompressed text into an array of strings
                return decompressedText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            }
        }
    }
}
