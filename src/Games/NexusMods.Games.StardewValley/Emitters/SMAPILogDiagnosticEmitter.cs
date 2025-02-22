using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.Loadouts;
using System.Runtime.CompilerServices;

namespace NexusMods.Games.StardewValley.Emitters;

[UsedImplicitly]
public class SMAPILogDiagnosticEmitter : ILoadoutDiagnosticEmitter
{
    private readonly ILogger _logger;

    public static readonly string SMAPILogsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValley", "ErrorLogs");
    public static readonly string SMAPILogFileName = "SMAPI-latest.txt";
    public static readonly string SMAPIErrorFileName = "SMAPI-crash.txt";
    public static readonly Uri LogUploadURL = new("https://smapi.io/log");
    public static readonly NamedLink SMAPILogsLink = new("SMAPI Log Uploader", LogUploadURL);
    public static readonly NamedLink NexusModsForumsLink = new("Nexus Mods forums", new Uri("https://forums.nexusmods.com/games/19-stardew-valley/"));
    public static readonly NamedLink SDVDiscordLink = new("Stardew Valley Discord server", new Uri("https://discord.gg/stardewvalley"));

    [UsedImplicitly]
    public SMAPILogDiagnosticEmitter(
        ILogger<SMAPILogDiagnosticEmitter> logger)
    {
        _logger = logger;
    }
    public async IAsyncEnumerable<Diagnostic> Diagnose(Loadout.ReadOnly loadout, [EnumeratorCancellation] CancellationToken cancellationToken)
    {

        await Task.Yield();

        LogFilePathWithEditTime? latestLog = GetLatestSMAPILogFile(_logger);

        _logger.LogDebug($"Latest SMAPI Log {latestLog}");

        if (latestLog == null)
        {
            _logger.LogDebug("No SMAPI logs available to perform diagnostic.");
            yield break;
        }

        // Ignore the regular logs, we're only interested in crashes.
        if (!latestLog.FilePath.EndsWith(SMAPIErrorFileName))
        {
            _logger.LogDebug("Last SMAPI run did not produce an error");
            yield break;
        }

        // Check if the last update was more than 12 hours ago. We can ignore it if so.
        TimeSpan timeAgo = DateTime.Now - latestLog.EditTime;
        if (timeAgo.TotalHours > 12)
        {
            _logger.LogDebug("Last SMAPI log was a crash, but it was over 12 hours ago so a diagnostic message was not raised.");
            yield break;
        }

        yield return Diagnostics.CreateGameRecentlyCrashed(
            LogPath: latestLog.FilePath,
            CrashTime: latestLog.EditTime.ToString(),
            SMAPILogLink: SMAPILogsLink,
            SDVDiscordLink: SDVDiscordLink,
            NexusModsForumsLink: NexusModsForumsLink
        );

    }

    public static LogFilePathWithEditTime? GetLatestSMAPILogFile(ILogger _logger)
    {
        // Check if the SMAPI logs folder exists (may require the game to be run at least once with SMAPI)
        if (!Directory.Exists(SMAPILogsFolder))
        {
            return null;
        }

        // Get all files in the folder
        string[] files = Directory.GetFiles(SMAPILogsFolder);

        // Find any SMAPI log files and sort by creation time.
        var logFiles = files
            .Where(file => file.EndsWith(SMAPILogFileName) || file.EndsWith(SMAPIErrorFileName))
            .Select(file => new LogFilePathWithEditTime(file, File.GetLastWriteTime(file)))
            .OrderBy(file => file.EditTime)
            .ToList();

        if (logFiles.Any())
        {
            // Return the newest log file.
            return logFiles.Last();

        }
        else
        {
            return null;
        }
    }

}

[UsedImplicitly]
public class LogFilePathWithEditTime(string filePath, DateTime editTime)
{
    public string FilePath { get; set; } = filePath;
    public DateTime EditTime { get; set; } = editTime;

    public override string ToString()
    {
        return $"File: {FilePath}, Last Edited: {EditTime}";
    }
}
