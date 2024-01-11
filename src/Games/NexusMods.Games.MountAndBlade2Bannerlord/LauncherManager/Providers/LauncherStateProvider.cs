using Bannerlord.LauncherManager.External;
using Bannerlord.LauncherManager.Models;
using Microsoft.Extensions.Options;
using NexusMods.Games.MountAndBlade2Bannerlord.Options;

namespace NexusMods.Games.MountAndBlade2Bannerlord.LauncherManager.Providers;

internal sealed class LauncherStateProvider : ILauncherStateProvider
{
    public string Executable { get; private set; } = string.Empty;
    public string ExecutableParameters { get; private set; } = string.Empty;

    private readonly IOptionsSnapshot<MountAndBlade2BannerlordOptions> _options;

    public LauncherStateProvider(IOptionsSnapshot<MountAndBlade2BannerlordOptions> options)
    {
        _options = options;
    }

    public void SetGameParameters(string executable, IReadOnlyList<string> gameParameters)
    {
        Executable = executable;
        ExecutableParameters = string.Join(" ", gameParameters);
    }

    public LauncherOptions GetOptions() => new()
    {
        Language = "English", // TODO: We'll need to map the localization system language from NMA to this
        UnblockFiles = _options.Value.UnblockFiles,
        FixCommonIssues = _options.Value.FixCommonIssues,
        BetaSorting = _options.Value.BetaSorting,
    };

    // TODO:
    public LauncherState GetState() => null!;
}
