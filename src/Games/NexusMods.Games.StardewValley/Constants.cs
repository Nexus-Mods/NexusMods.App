using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.Games.StardewValley;

public static class Constants
{
    public static readonly RelativePath ModsFolder = "Mods".ToRelativePath();
    public static readonly RelativePath ManifestFile = "manifest.json".ToRelativePath();
    public static readonly string SMAPILogsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValley", "ErrorLogs");
    public static readonly string SMAPILogFileName = "SMAPI-latest.txt";
    public static readonly string SMAPIErrorFileName = "SMAPI-crash.txt";
    public static readonly Uri LogUploadURL = new("https://smapi.io/log");
    public static readonly RelativePath ModsFolder = "Mods";
    public static readonly RelativePath ContentFolder = "Content";
    public static readonly RelativePath ManifestFile = "manifest.json";
}
