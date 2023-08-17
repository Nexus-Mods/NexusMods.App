using NexusMods.Paths;

namespace NexusMods.Benchmarks;

public class Assets
{
    public static string ProgramFolder => FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory).ToString();
    public static string AssetsFolder => Path.Combine(ProgramFolder, "Assets");
    // ReSharper disable once InconsistentNaming
    public static string Sample7zFile => Path.Combine(AssetsFolder, "data_7zip_lzma2.7z");
    public static string SampleZipFile => Path.Combine(AssetsFolder, "data_zip_lzma.zip");
}
