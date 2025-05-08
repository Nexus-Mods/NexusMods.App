using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.Loadouts;
using NexusMods.CrossPlatform.Process;
using NexusMods.Generators.Diagnostics;
using NexusMods.Paths;
using static NexusMods.Games.RedEngine.Constants;
namespace NexusMods.Games.RedEngine.Cyberpunk2077.Emitters;

public partial class MissingProtontricksForRedModEmitter : ILoadoutDiagnosticEmitter
{
    public static readonly NamedLink ProtontricksLink = new("Protontricks Installation Guide", new Uri("https://github.com/Matoking/protontricks?tab=readme-ov-file#installation"));
    
    /// <summary>
    /// This will be null on non-Linux OSes.
    /// </summary>
    private AggregateProtontricksDependency? _protontricksDependency;
    
    /// <summary/>
    public MissingProtontricksForRedModEmitter(IServiceProvider serviceProvider) => _protontricksDependency = serviceProvider.GetService<AggregateProtontricksDependency>();

    public async IAsyncEnumerable<Diagnostic> Diagnose(
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken)
    {
        var install = loadout.InstallationInstance;
        var locations = install.LocationsRegister;
        var redModPath = locations.GetResolvedPath(RedModPath);

        if (!FileSystem.Shared.OS.IsLinux || _protontricksDependency == null)
            yield break;

        // If there is no REDmod EXE, we don't need Protontricks.
        if (redModPath.FileExists)
            yield break;

        var installInfo = await _protontricksDependency.QueryInstallationInformation(cancellationToken);
        if (!installInfo.HasValue)
            yield return Diagnostics.CreateMissingProtontricksForRedMod(ProtontricksLink);
    }
}
