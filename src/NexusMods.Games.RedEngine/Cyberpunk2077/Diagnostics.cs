using JetBrains.Annotations;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.RedEngine.Cyberpunk2077.Emitters;
using NexusMods.Generators.Diagnostics;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.Cyberpunk2077;

public static partial class Diagnostics
{
    public const string Source = "NexusMods.Games.RedEngine.Cyberpunk2077";

    [DiagnosticTemplate] 
    [UsedImplicitly] 
    internal static IDiagnosticTemplate MissingModWithKnownNexusUri = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 1))
        .WithTitle("Missing Dependency")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("'{Group}' requires '{DependencyName}' which is not installed")
        .WithDetails("""
The mod **{Group}** requires **{DependencyName}** to function, but **{DependencyName}** is not installed.


### How to Resolve
1. Download **{DependencyName}** from {NexusModsDependencyUri}
2. Add **{DependencyName}** to the loadout. 

OR 

1. Remove **{Group}** from "Installed Mods"

### Technical Details
{Explanation}

**{Group}** includes the file `{Path}` in `{SearchPath}` (or one of the subfolders) and has the extension `{SearchExtension}` which indicates that the **{DependencyName}** mod is required.
""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<LoadoutItemGroupReference>("Group")
            .AddValue<string>("DependencyName")
            .AddValue<NamedLink>("NexusModsDependencyUri")
            .AddValue<string>("Explanation")
            .AddValue<GamePath>("Path")
            .AddValue<GamePath>("SearchPath")
            .AddValue<Extension>("SearchExtension")
        )
        .Finish();

    
    [DiagnosticTemplate] 
    [UsedImplicitly] 
    internal static IDiagnosticTemplate MissingModWithKnownNexusUriWithStringSegment = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 1))
        .WithTitle("Missing Dependency")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("'{Group}' requires '{DependencyName}' which is not installed")
        .WithDetails("""
The mod **{Group}** requires **{DependencyName}** to function, but **{DependencyName}** is not installed.


### How to Resolve
1. Download **{DependencyName}** from {NexusModsDependencyUri}
2. Add **{DependencyName}** to the loadout. 

OR 

1. Remove **{Group}** from "Installed Mods"

### Technical Details
{Explanation}

**{Group}** includes the file `{Path}` in `{SearchPath}` (or one of the subfolders) and has the extension `{SearchExtension}`. The file has a reference to **{DependencyName}** on line {LineNumber} indicating that the **{DependencyName}** mod is required.

```
{MatchingSegment}
```
""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<LoadoutItemGroupReference>("Group")
            .AddValue<string>("DependencyName")
            .AddValue<NamedLink>("NexusModsDependencyUri")
            .AddValue<string>("Explanation")
            .AddValue<GamePath>("Path")
            .AddValue<GamePath>("SearchPath")
            .AddValue<Extension>("SearchExtension")
            .AddValue<string>("MatchingSegment")
            .AddValue<int>("LineNumber")
        )
        .Finish();

    [DiagnosticTemplate] 
    [UsedImplicitly] 
    internal static IDiagnosticTemplate DisabledGroupDependency = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 2))
        .WithTitle("Disabled Dependency")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("'{Group}' requires '{DependencyName}' but it is disabled")
        .WithDetails("""
The mod **{Group}** requires **{DependencyName}** to function, but **{DependencyGroup}** is not enabled.


### How to Resolve
1. Enable **{DependencyGroup}** in "Installed Mods"

OR

1. Remove **{Group}** from "Installed Mods"

### Technical Details
{Explanation}

**{Group}** includes the file `{Path}` in `{SearchPath}` (or one of the subfolders) and has the extension `{SearchExtension}` which indicates that the **{DependencyName}** mod is required.

""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<LoadoutItemGroupReference>("Group")
            .AddDataReference<LoadoutItemGroupReference>("DependencyGroup")
            .AddValue<string>("DependencyName")
            .AddValue<string>("Explanation")
            .AddValue<GamePath>("Path")
            .AddValue<GamePath>("SearchPath")
            .AddValue<Extension>("SearchExtension")
        )
        .Finish();
    
    [DiagnosticTemplate] 
    [UsedImplicitly] 
    internal static IDiagnosticTemplate DisabledGroupDependencyWithStringSegment = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 2))
        .WithTitle("Disabled Mod Dependency")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("'{Group}' requires '{DependencyName}' but it is disabled")
        .WithDetails("""
The mod **{Group}** requires **{DependencyName}** to function, but **{DependencyGroup}** is not enabled.


### How to Resolve
1. Enable **{DependencyGroup}** in "Installed Mods"

OR

1. Remove **{Group}** from "Installed Mods"

### Technical Details
{Explanation}

**{Group}** includes the file `{Path}` in `{SearchPath}` (or one of the subfolders) and has the extension `{SearchExtension}`. The file has a reference to **{DependencyName}** on line {LineNumber} indicating that the **{DependencyName}** mod is required.

```
{MatchingSegment}
```
""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<LoadoutItemGroupReference>("Group")
            .AddDataReference<LoadoutItemGroupReference>("DependencyGroup")
            .AddValue<string>("DependencyName")
            .AddValue<string>("Explanation")
            .AddValue<GamePath>("Path")
            .AddValue<GamePath>("SearchPath")
            .AddValue<Extension>("SearchExtension")
            .AddValue<string>("MatchingSegment")
            .AddValue<int>("LineNumber")
        )
        .Finish();
    
    [DiagnosticTemplate]
    [UsedImplicitly]
    internal static IDiagnosticTemplate MissingProtontricksForRedMod = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 3))
        .WithTitle("Protontricks is not installed")
        .WithSeverity(DiagnosticSeverity.Critical)
        .WithSummary("Protontricks is required to use REDmods but is not present.")
        .WithDetails("""
Protontricks is not installed by is required to use the REDmod DLC when playing Cyberpunk 2077 on Linux.

### How to Resolve
1. Install Protontricks by following the instructions in the {ProtontricksUri}.
2. Apply a loadout with Protontricks installed.

OR

1. Remove any mods requiring REDmod from "Installed Mods".

### Technical Details
The OS did not return any information when querying for Protontricks installation information. This indicates that Protontricks is not installed.
""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<NamedLink>("ProtontricksUri")
        )
        .Finish();
    
    [DiagnosticTemplate]
    [UsedImplicitly]
    internal static IDiagnosticTemplate MissingRedModDependency = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 4))
        .WithTitle("REDmod DLC is not installed")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("The official REDmod DLC is required for {ModCount} mod(s) but is not installed.")
        .WithDetails("""
The official REDmod DLC is a mod loader required to use some mods for Cyberpunk 2077. The loadout contains {ModCount} mod(s) that require REDmod to work, but it is not installed.

### How to Resolve
1. Install the REDmod DLC - View in {RedmodLink} or the {GenericLink}.
2. Apply a loadout with REDmod installed.

OR

1. Remove any mods requring REDmod from Installed Mods.
2. Remove any subfolders of `{RedModFolder}` from External Changes.

### Technical Details 
Each subfolder in the `{RedModFolder}` represents an installed mod. If any folders are present, REDmod is required. 

After finding {ModCount} mod(s), the REDmod DLC was checked for installation by verifying that `{RedModEXE}` exists.

""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<NamedLink>("RedmodLink")
            .AddValue<NamedLink>("GenericLink")
            .AddValue<int>("ModCount")
            .AddValue<string>("RedModFolder")
            .AddValue<string>("RedModEXE")
        )
        .Finish();

    [DiagnosticTemplate]
    [UsedImplicitly]
    internal static IDiagnosticTemplate RequiredWineDllOverrides = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 5))
        .WithTitle("Incorrect WINE configuration")
        .WithSeverity(DiagnosticSeverity.Critical)
        .WithSummary("Update the WINE DLL overrides to mod on Linux")
        .WithDetails("""
Modding Cyberpunk 2077 on Linux requires you to configure WINE or the game will not launch correctly.

### How to Resolve
{Instructions}

### Technical Details
The current WINE DLL overrides are not set up correctly for modding on Linux.
""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<string>("Instructions")
        )
        .Finish();

    [DiagnosticTemplate]
    [UsedImplicitly]
    internal static IDiagnosticTemplate RequiredWinetricksPackages = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 6))
        .WithTitle("Missing Winetricks packages")
        .WithSeverity(DiagnosticSeverity.Critical)
        .WithSummary("Install required packages to mod on Linux")
        .WithDetails("""
Modding Cyberpunk 2077 on Linux requires packages to be installed in your WINE prefix.

### How to Resolve
{Instructions}  

### Technical Details
The current WINE prefix does not have the required packages installed for modding on Linux.
""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<string>("Instructions")
        )
        .Finish();
}
