using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.CLI;

namespace NexusMods.Updater.Verbs;

public class ForceAppUpdate : AVerb
{
    private readonly UpdaterService _updater;
    private readonly ILogger<ForceAppUpdate> _logger;

    public ForceAppUpdate(ILogger<ForceAppUpdate> logger,  UpdaterService updater)
    {
        _logger = logger;
        _updater = updater;
    }

    public static VerbDefinition Definition => new("force-app-update",
        "Forces a download of the latest version of the app", Array.Empty<OptionDefinition>());

    public async Task<int> Run(CancellationToken token)
    {
        _logger.LogInformation("Forcing an update...");
        await _updater.DownloadAndExtractUpdate(new Version(0, 0, 0, 1));
        _logger.LogInformation("Update complete. The next UI startup will boot into the new version");
        return 0;
    }

}
