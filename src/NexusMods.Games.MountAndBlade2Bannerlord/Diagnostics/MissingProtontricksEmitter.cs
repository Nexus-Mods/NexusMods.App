using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.Generic.Dependencies;
using NexusMods.Paths;
namespace NexusMods.Games.MountAndBlade2Bannerlord.Diagnostics;

public partial class MissingProtontricksEmitter : ILoadoutDiagnosticEmitter
{
    public static readonly NamedLink ProtontricksLink = new("Protontricks Installation Guide", new Uri("https://github.com/Matoking/protontricks?tab=readme-ov-file#installation"));
    
    /// <summary>
    /// This will be null on non-Linux OSes.
    /// </summary>
    private readonly AggregateProtontricksDependency? _protontricksDependency;
    
    /// <summary/>
    public MissingProtontricksEmitter(IServiceProvider serviceProvider) => _protontricksDependency = serviceProvider.GetService<AggregateProtontricksDependency>();

    public async IAsyncEnumerable<Diagnostic> Diagnose(
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken)
    {
        if (!FileSystem.Shared.OS.IsLinux || _protontricksDependency == null)
            yield break;

        if (loadout.Installation.Store == GameStore.Steam)
        {
            var installInfo = await _protontricksDependency.QueryInstallationInformation(cancellationToken);
            if (!installInfo.HasValue)
                yield return Diagnostics.CreateMissingProtontricksForRedMod(ProtontricksLink);
        }
    }
}
