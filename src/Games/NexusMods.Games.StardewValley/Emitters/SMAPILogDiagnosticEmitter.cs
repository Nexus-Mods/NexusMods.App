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

    public static readonly NamedLink SMAPILogsLink = new("SMAPI Log Uploader", Constants.LogUploadURL);
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

        List<LogFilePathWithEditTime>? allLogs = Helpers.GetLatestSMAPILogFile(_logger);

        _logger.LogDebug($"Latest SMAPI Log {allLogs?.Last()}");

        if (allLogs == null)
        {
            _logger.LogDebug("No SMAPI logs available to perform diagnostic.");
            yield break;
        }

        LogFilePathWithEditTime latestLog = allLogs.Last();

        // Ignore the regular logs, we're only interested in crashes.
        if (!latestLog.FilePath.EndsWith(Constants.SMAPIErrorFileName))
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

}
