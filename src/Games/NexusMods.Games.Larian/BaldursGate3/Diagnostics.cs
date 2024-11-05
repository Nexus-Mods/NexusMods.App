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
        .WithSummary("The mod {ModName} is missing the required dependency '{MissingDepName}' v{MissingDepVersion}+.")
        .WithDetails("""
                     '{MissingDepName}' v{MissingDepVersion}+ is not installed or enabled in your Loadout. This pak module is required by '{PakModuleName}' v{PakModuleVersion} to run correctly.
                     
                     
                     ## Recommended Actions
                     #### Search for and install the missing mod
                     You can search for '{MissingDepName}' on {NexusModsLink} or search the in-game mod manager.
                     #### Or
                     #### Check the required mods section on {ModName} Nexus Mods page
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
    
    [DiagnosticTemplate]
    [UsedImplicitly]
    internal static IDiagnosticTemplate OutdatedDependencyTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 1))
        .WithTitle("Required dependency is outdated")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("Mod {ModName} requires at least version {MinDepVersion}+ of '{DepName}' but only v{CurrentDepVersion} is installed.")
        .WithDetails("""
                     '{PakModuleName}' v{PakModuleVersion} requires at least version {MinDepVersion}+ of '{DepName}' to run correctly. However, you only have version v{CurrentDepVersion} installed in mod {ModName}.
                     """)
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<LoadoutItemGroupReference>("ModName")
            .AddValue<string>("PakModuleName")
            .AddValue<string>("PakModuleVersion")
            .AddDataReference<LoadoutItemGroupReference>("DepModName")
            .AddValue<string>("DepName")
            .AddValue<string>("MinDepVersion")
            .AddValue<string>("CurrentDepVersion")
        )
        .Finish();
    
    
    [DiagnosticTemplate]
    [UsedImplicitly]
    internal static IDiagnosticTemplate InvalidPakFileTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 1))
        .WithTitle("Invalid pak file")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("Invalid .pak File Detected in {ModName}")
        .WithDetails("""
                     The mod contains a .pak file, typically used to store mod data for Baldur's Gate 3. However,
                     this one appears to be invalid or incompatible: '{PakFileName}'.
                     
                     
                     ## Recommended Actions
                     Verify that the file is installed in the intended location and that it wasn't altered or corrupted. You may need to remove or reinstall the mod, consulting the mod's instructions for proper installation.
                     """)
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<LoadoutItemGroupReference>("ModName")
            .AddValue<string>("PakFileName")
        )
        .Finish();
    
}
