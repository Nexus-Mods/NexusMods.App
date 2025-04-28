using JetBrains.Annotations;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Generators.Diagnostics;


namespace NexusMods.Games.Larian.BaldursGate3;

internal static partial class Diagnostics
{
    private const string Source = "NexusMods.Games.Larian.BaldursGate3";

    [DiagnosticTemplate] [UsedImplicitly] internal static IDiagnosticTemplate MissingDependencyTemplate = DiagnosticTemplateBuilder
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
                     """
        )
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<LoadoutItemGroupReference>("ModName")
            .AddValue<string>("MissingDepName")
            .AddValue<string>("MissingDepVersion")
            .AddValue<string>("PakModuleName")
            .AddValue<string>("PakModuleVersion")
            .AddValue<NamedLink>("NexusModsLink")
        )
        .Finish();

    [DiagnosticTemplate] [UsedImplicitly] internal static IDiagnosticTemplate OutdatedDependencyTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 2))
        .WithTitle("Required dependency is outdated")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("Mod {ModName} requires at least version {MinDepVersion}+ of '{DepName}' but only v{CurrentDepVersion} is installed.")
        .WithDetails("""
                     '{PakModuleName}' v{PakModuleVersion} requires at least version {MinDepVersion}+ of '{DepName}' to run correctly. However, you only have version v{CurrentDepVersion} installed in mod {ModName}.
                     """
        )
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


    [DiagnosticTemplate] [UsedImplicitly] internal static IDiagnosticTemplate InvalidPakFileTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 3))
        .WithTitle("Invalid pak file")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("Invalid .pak File Detected in {ModName}")
        .WithDetails("""
                     The mod contains a .pak file, typically used to store mod data for Baldur's Gate 3. However,
                     this one appears to be invalid or incompatible: '{PakFileName}'.
                     
                     
                     ## Recommended Actions
                     Verify that the file is installed in the intended location and that it wasn't altered or corrupted. You may need to remove or reinstall the mod, consulting the mod's instructions for proper installation.
                     """
        )
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<LoadoutItemGroupReference>("ModName")
            .AddValue<string>("PakFileName")
        )
        .Finish();

    [DiagnosticTemplate] [UsedImplicitly] internal static IDiagnosticTemplate MissingRequiredScriptExtenderTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 4))
        .WithTitle("Missing Script Extender")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("Missing BG3 Script Extender, required by {ModName}")
        .WithDetails("""
                     The .pak file {PakName} lists the Baldur's Gate 3 Script Extender (BG3SE) as a dependency, but it isn't installed.
                     
                     ## Recommended Actions
                     Install the BG3 Script Extender from {BG3SENexusLink} or from the official source.
                     """
        )
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<LoadoutItemGroupReference>("ModName")
            .AddValue<string>("PakName")
            .AddValue<NamedLink>("BG3SENexusLink")
        )
        .Finish();
    
    [DiagnosticTemplate] [UsedImplicitly] internal static IDiagnosticTemplate Bg3SeWineDllOverrideSteamTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 5))
        .WithTitle("BG3SE Wine DLL Override required for Steam")
        .WithSeverity(DiagnosticSeverity.Suggestion)
        .WithSummary("BG3SE Requires WINEDLLOVERRIDE Environment variable to be set")
        .WithDetails("""
                     In Linux Wine environments, the BG3 Script Extender (BG3SE) requires WINEDLLOVERRIDE environment to contain `"DWrite=n,b"` to work correctly.
                     Please ensure you have this set correctly in your BG3 Steam properties, under Launch Options:
                     `WINEDLLOVERRIDES="DWrite=n,b" %command%`
                     
                     
                     ## Details:
                     BG3SE adds `DWrite.dll` file to the game folder, which replaces a Windows system dll normally located in windows system folders. 
                     On windows the game will automatically load the dll file from the game folder if present, preferring that over the system one.
                     On Wine, to achieve the same effect, you need to set the WINEDLLOVERRIDE environment variable to tell Wine to load the game's DWrite.dll instead of the system one.
                     """
        
        )
        .WithMessageData(messageBuilder => messageBuilder.AddValue<string>("Template"))
        .Finish();
}
