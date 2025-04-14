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
        .WithTitle("Missing Mod with Known Nexus URI")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("{DependencyName} is not installed")
        .WithDetails("""
We've detected that the mod `{Group}` contains files that require {DependencyName} to function properly, but it is not installed. You can download the latest 
version of {DependencyName} from {NexusModsDependencyUri}.

{Explanation}

The file `{Path}` is in `{SearchPath}` and has the extension `{SearchExtension}` so we know that the mod `{DependencyName}` is required.
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
        .WithTitle("Missing Mod with Known Nexus URI")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("{DependencyName} is not installed")
        .WithDetails("""
We've detected that the mod `{Group}` contains files that require {DependencyName} to function properly, but it is not installed. You can download the latest 
version of {DependencyName} from {NexusModsDependencyUri}.

{Explanation}

The file `{Path}` is in `{SearchPath}` and has the extension `{SearchExtension}` so we looked in the file and found (at line {LineNumber})
the following usecase of {DependencyName}

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
        .WithTitle("Disabled Mod Dependency")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("{DependencyName} is disabled")
        .WithDetails("""
We've detected that the mod `{Group}` contains files that require {DependencyName} to function properly, but it is not enabled. Please 
re-enable `{DependencyGroup}` to resolve this issue.

{Explanation}

The file `{Path}` is in `{SearchPath}` and has the extension `{SearchExtension}` so we know that the mod `{DependencyName}` is required.
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
        .WithSummary("{DependencyName} is disabled")
        .WithDetails("""
We've detected that the mod `{Group}` contains files that require {DependencyName} to function properly, but it is not enabled. Please 
re-enable `{DependencyGroup}` to resolve this issue.

{Explanation}

The file `{Path}` is in `{SearchPath}` and has the extension `{SearchExtension}` so we looked in the file and found (at line {LineNumber})
the following usecase of {DependencyName}

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
        .WithTitle("Missing 'Protontricks' dependency")
        .WithSeverity(DiagnosticSeverity.Critical)
        .WithSummary("Protontricks is required to install REDmods but is not present.")
        .WithDetails("""
Protontricks is required to install REDmods but is not present.

Refer to the {ProtontricksUri} for installation instructions.
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
        .WithTitle("Missing 'REDmod' DLC")
        .WithSeverity(DiagnosticSeverity.Critical)
        .WithSummary("REDmod DLC is required to install REDmods but is not present.")
        .WithDetails("""
The {RedmodLink} is required to install REDmods but is not present in your game folder.

Please install the {RedmodLink} and start the game at least once.
""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<NamedLink>("RedmodLink")
        )
        .Finish();

    [DiagnosticTemplate]
    [UsedImplicitly]
    internal static IDiagnosticTemplate RequiredWineDllOverrides = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 5))
        .WithTitle("Update WINE dll overrides to mod on Linux")
        .WithSeverity(DiagnosticSeverity.Critical)
        .WithSummary("These changes are required for Linux")
        .WithDetails("""
Modding Cyberpunk 2077 on Linux requires you to configure WINE.

{Instructions}
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
        .WithTitle("Install required winetricks packages")
        .WithSeverity(DiagnosticSeverity.Critical)
        .WithSummary("These packages are required or Linux")
        .WithDetails("""
Modding Cyberpunk 2077 on Linux requires packages to be installed in your WINE prefix.

{Instructions}  
""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<string>("Instructions")
        )
        .Finish();
}
