using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.Generic;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.Emitters;

public class WinePrefixRequirementsEmitter : ILoadoutDiagnosticEmitter
{
    // https://wiki.redmodding.org/cyberpunk-2077-modding/for-mod-users/users-modding-cyberpunk-2077/modding-on-linux
    private static readonly WineDllOverride[] RequiredOverrides =
    [
        new("winmm", [WineDllOverrideType.Native, WineDllOverrideType.BuiltIn]),
        new("version", [WineDllOverrideType.Native, WineDllOverrideType.BuiltIn]),
    ];

    private static readonly ImmutableHashSet<string> RequiredWinetricksPackages =
    [
        "d3dcompiler_47",
        "vcrun2022",
    ];

    public async IAsyncEnumerable<Diagnostic> Diagnose(Loadout.ReadOnly loadout, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();

        var dllOverridesInstructions = WineDiagnosticHelper.GetWineDllOverridesUpdateInstructions(loadout.InstallationInstance, RequiredOverrides);
        if (dllOverridesInstructions is not null) yield return Diagnostics.CreateRequiredWineDllOverrides(dllOverridesInstructions);

        var winetricksInstructions = WineDiagnosticHelper.GetWinetricksInstructions(loadout.InstallationInstance, RequiredWinetricksPackages);
        if (winetricksInstructions is not null) yield return Diagnostics.CreateRequiredWinetricksPackages(winetricksInstructions);
    }
}
