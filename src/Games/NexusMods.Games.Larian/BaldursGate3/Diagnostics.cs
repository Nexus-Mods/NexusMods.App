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
        .WithSummary("The mod `{ModName}` is missing the required dependency '{MissingDepName}' v{MissingDepVersion}.")
        .WithDetails("""
                     
                     '{MissingDependencyName}' v{MissingDevVersion} is not installed or enabled in your Loadout. This pak module is required by `{PakModuleName}` v{PakModuleVersion} to run correct.
                     
                     ## Recommended actions
                     ### Search for and install the missing mod
                     You can search for '{MissingDependencyName}' on {NexusModsLink}
                     
                     
                     You can try to search the missing mod on {NexusModsLink} or using the in-game mod manager.
                     """)
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<LoadoutItemGroupReference>("ModName")
            .AddValue<string>("MissingDepName")
            .AddValue<string>("PakModuleName")
            .AddValue<string>("PakModuleVersion")
            .AddValue<string>("MissingDepVersion")
            .AddValue<NamedLink>("NexusModsLink")
        )
        .Finish();
    
}
