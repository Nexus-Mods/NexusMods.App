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
        .WithSummary("The mod {ModName} is missing the required dependency '{MissingDepName}' v{MissingDepVersion}.")
        .WithDetails("""
                     '{MissingDepName}' v{MissingDepVersion} is not installed or enabled in your Loadout. This pak module is required by '{PakModuleName}' v{PakModuleVersion} to run correctly.
                     
                     
                     ## Recommended actions
                     #### Search for and install the missing mod
                     You can search for '{MissingDepName}' on {NexusModsLink} or search the in-game mod manager.
                     #### Or
                     #### Check the required mods section on {ModName} NexusMods page
                     Mod pages can contain useful installation instructions in the 'Description' tab, this tab will also include requirements the mod needs to work correctly. 
                     """)
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<LoadoutItemGroupReference>("ModName")
            .AddValue<string>("MissingDepName")
            .AddValue<string>("MissingDepVersion")
            .AddValue<string>("PakModuleName")
            .AddValue<string>("PakModuleVersion")
            .AddValue<NamedLink>("NexusModsLink")
        )
        .Finish();
    
}
