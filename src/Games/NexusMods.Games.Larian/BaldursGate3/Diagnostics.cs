using JetBrains.Annotations;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Generators.Diagnostics;


namespace NexusMods.Games.Larian.BaldursGate3;

internal static partial class Diagnostics
{
    private const string Source = "NexusMods.Games.Larian.BaldursGate3";
    
    [DiagnosticTemplate]
    [UsedImplicitly]
    internal static IDiagnosticTemplate MissingDependencyTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 1))
        .WithTitle("Missing required dependency")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("Mod {PakMod} is missing required dependency '{MissingDependencyName}'.")
        .WithDetails("""
                     '{MissingDependencyName}' is required by '{PakModuleName}' but is not present in the loadout.
                     
                     You can try to search the missing mod on {NexusModsLink} or using the in-game mod manager.
                     """)
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<LoadoutItemGroupReference>("PakMod")
            .AddValue<string>("MissingDependencyName")
            .AddValue<string>("PakModuleName")
            .AddValue<NamedLink>("NexusModsLink")
        )
        .Finish();
    
}
