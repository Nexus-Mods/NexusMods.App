using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.Loadouts;
using NexusMods.CrossPlatform.Process;
using NexusMods.Generators.Diagnostics;
using NexusMods.Paths;
namespace NexusMods.Games.RedEngine.Cyberpunk2077.Emitters;

public partial class MissingProtontricksForRedModEmitter : ILoadoutDiagnosticEmitter
{
    public static readonly NamedLink ProtontricksLink = new("Protontricks Installation Guide", new Uri("https://github.com/Matoking/protontricks?tab=readme-ov-file#installation"));
    
    /// <summary>
    /// This will be null on non-Linux OSes.
    /// </summary>
    private ProtontricksDependency? _protontricksDependency;
    
    /// <summary/>
    public MissingProtontricksForRedModEmitter(IServiceProvider serviceProvider) => _protontricksDependency = serviceProvider.GetService<ProtontricksDependency>();

    public async IAsyncEnumerable<Diagnostic> Diagnose(
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken)
    {
        if (!FileSystem.Shared.OS.IsLinux || _protontricksDependency == null)
            yield break;

        var installInfo = await _protontricksDependency.QueryInstallationInformation(cancellationToken);
        if (!installInfo.HasValue)
            yield return Diagnostics.CreateMissingProtontricksForRedMod(ProtontricksLink);
    }
}
