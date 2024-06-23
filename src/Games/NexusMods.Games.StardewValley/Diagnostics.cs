using JetBrains.Annotations;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Generators.Diagnostics;

namespace NexusMods.Games.StardewValley;

internal static partial class Diagnostics
{
    private const string Source = "NexusMods.Games.StardewValley";

    [DiagnosticTemplate]
    [UsedImplicitly]
    internal static IDiagnosticTemplate MissingRequiredDependencyTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 1))
        .WithTitle("Missing required dependency")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("Mod {Mod} is missing required dependency '{MissingDependencyModName}'")
        .WithDetails("""
You can download the latest version of '{MissingDependencyModName}' (`{MissingDependencyModId}`) at {NexusModsDependencyUri}.
""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<ModReference>("Mod")
            .AddValue<string>("MissingDependencyModName")
            .AddValue<string>("MissingDependencyModId")
            .AddValue<NamedLink>("NexusModsDependencyUri")
        )
        .Finish();

    [DiagnosticTemplate]
    [UsedImplicitly]
    internal static IDiagnosticTemplate RequiredDependencyIsOutdatedTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 2))
        .WithTitle("Required dependency is outdated")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("Mod {Dependent} requires at least version {MinimumVersion} of {Dependency} but installed is {CurrentVersion}")
        .WithDetails("You can download the latest version at {NexusModsDependencyUri}")
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<ModReference>("Dependent")
            .AddDataReference<ModReference>("Dependency")
            .AddValue<string>("MinimumVersion")
            .AddValue<string>("CurrentVersion")
            .AddValue<NamedLink>("NexusModsDependencyUri")
        )
        .Finish();

    [DiagnosticTemplate]
    [UsedImplicitly]
    internal static IDiagnosticTemplate SMAPIRequiredButNotInstalledTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 3))
        .WithTitle("SMAPI is not installed")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("SMAPI is required for {ModCount} Mod(s) but it's not installed")
        .WithDetails("You can install the latest SMAPI version at {NexusModsSMAPIUri}.")
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<int>("ModCount")
            .AddValue<NamedLink>("NexusModsSMAPIUri")
        )
        .Finish();

    [DiagnosticTemplate]
    [UsedImplicitly]
    internal static IDiagnosticTemplate SMAPIRequiredButDisabledTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 4))
        .WithTitle("SMAPI is not enabled")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("SMAPI is required for {ModCount} Mod(s) but it's not enabled")
        .WithoutDetails()
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<int>("ModCount")
        )
        .Finish();

    [DiagnosticTemplate]
    [UsedImplicitly]
    internal static IDiagnosticTemplate DisabledRequiredDependencyTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 5))
        .WithTitle("Required dependency is disabled")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("Mod {Mod} requires {Dependency} to be enabled")
        .WithoutDetails()
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<ModReference>("Mod")
            .AddDataReference<ModReference>("Dependency")
        )
        .Finish();

    [DiagnosticTemplate]
    [UsedImplicitly]
    internal static IDiagnosticTemplate ModCompatabilityObsoleteTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 6))
        .WithTitle("Mod is obsolete")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("Mod {Mod} is obsolete")
        .WithDetails("""
Mod {Mod} has been made obsolete:

> {ModName} is obsolete because {ReasonPhrase}

The compatibility status was extracted from the internal SMAPI metadata file.
""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<ModReference>("Mod")
            .AddValue<string>("ModName")
            .AddValue<string>("ReasonPhrase")
        )
        .Finish();

    [DiagnosticTemplate]
    [UsedImplicitly]
    internal static IDiagnosticTemplate ModCompatabilityAssumeBrokenTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 7))
        .WithTitle("Mod is assumed broken")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("Mod {Mod} is assumed broken")
        .WithDetails("""
Mod {Mod} is marked as broken by SMAPI:

> {ReasonPhrase}

Please check for a version newer than {ModVersion} at {ModLink}.

The compatibility status was extracted from the internal SMAPI metadata file.
""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<ModReference>("Mod")
            .AddValue<string>("ReasonPhrase")
            .AddValue<NamedLink>("ModLink")
            .AddValue<string>("ModVersion")
        )
        .Finish();

    [DiagnosticTemplate]
    [UsedImplicitly]
    internal static IDiagnosticTemplate GameVersionOlderThanMinimumGameVersionTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 8))
        .WithTitle("Game Version older than supported by SMAPI")
        .WithSeverity(DiagnosticSeverity.Critical)
        .WithSummary("The minimum supported game version of SMAPI {SMAPIVersion} is {MinimumGameVersion}")
        .WithDetails("""
SMAPI version {SMAPIVersion} requires the game version to be at least {MinimumGameVersion}.
The current game version is {CurrentGameVersion}.

Due to this version mismatch, the game will **crash** on startup.
You can solve this issue by either updating your game or downgrading SMAPI.

The newest supported SMAPI version for game version {CurrentGameVersion} is {NewestSupportedSMAPIVersionForCurrentGameVersion}.
You can download this SMAPI version from {SMAPINexusModsLink}.
""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<ModReference>("SMAPIMod")
            .AddValue<string>("SMAPIVersion")
            .AddValue<string>("MinimumGameVersion")
            .AddValue<string>("CurrentGameVersion")
            .AddValue<string>("NewestSupportedSMAPIVersionForCurrentGameVersion")
            .AddValue<NamedLink>("SMAPINexusModsLink")
        )
        .Finish();

    [DiagnosticTemplate]
    [UsedImplicitly]
    internal static IDiagnosticTemplate GameVersionNewerThanMaximumGameVersionTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 9))
        .WithTitle("Game Version newer than supported by SMAPI")
        .WithSeverity(DiagnosticSeverity.Critical)
        .WithSummary("The maximum supported game version of SMAPI {SMAPIVersion} is {MaximumGameVersion}")
        .WithDetails("""
SMAPI version {SMAPIVersion} requires the game version to be lower than {MaximumGameVersion}.
The current game version is {CurrentGameVersion}.

Due to this version mismatch, the game will **crash** on startup.
You can solve this issue by either downgrading your game to {MaximumGameVersion} or updating SMAPI.

The newest supported SMAPI version for game version {CurrentGameVersion} is {NewestSupportedSMAPIVersionForCurrentGameVersion}.
You can download this SMAPI version from {SMAPINexusModsLink}.
""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<ModReference>("SMAPIMod")
            .AddValue<string>("SMAPIVersion")
            .AddValue<string>("MaximumGameVersion")
            .AddValue<string>("CurrentGameVersion")
            .AddValue<string>("NewestSupportedSMAPIVersionForCurrentGameVersion")
            .AddValue<NamedLink>("SMAPINexusModsLink")
        )
        .Finish();

    [DiagnosticTemplate]
    [UsedImplicitly]
    internal static IDiagnosticTemplate SuggestSMAPIVersionTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 10))
        .WithTitle("Install SMAPI to get started with modding Stardew Valley")
        .WithSeverity(DiagnosticSeverity.Suggestion)
        .WithSummary("Install SMAPI to get started with modding Stardew Valley")
        .WithDetails("""
SMAPI is the mod loader for Stardew Valley. The majority of mods require SMAPI to be installed.

You can download the latest supported SMAPI version {LatestSMAPIVersion} for your game version
{CurrentGameVersion} from {SMAPINexusModsLink}.

Once downloaded, add SMAPI to your mod list from the Library.
"""
        )
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<string>("LatestSMAPIVersion")
            .AddValue<string>("CurrentGameVersion")
            .AddValue<NamedLink>("SMAPINexusModsLink")
        )
        .Finish();

    [DiagnosticTemplate]
    [UsedImplicitly]
    internal static IDiagnosticTemplate SMAPIVersionOlderThanMinimumAPIVersion = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 11))
        .WithTitle("SMAPI Version newer than supported Mod")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("The minimum supported SMAPI version of {ModName} is {MinimumAPIVersion}")
        .WithDetails("""
Mod {Mod} requires the SMAPI version to be at least {MinimumAPIVersion}.
The current SMAPI version is {CurrentSMAPIVersion}.

You can solve this issue by either updating SMAPI or download an older version
of the mod from {NexusModsLink}. The latest SMAPI version can be downloaded
from {SMAPINexusModsLink}.
""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<ModReference>("Mod")
            .AddValue<string>("ModName")
            .AddValue<string>("MinimumAPIVersion")
            .AddValue<string>("CurrentSMAPIVersion")
            .AddValue<NamedLink>("NexusModsLink")
            .AddValue<NamedLink>("SMAPINexusModsLink")
        )
        .Finish();

    [DiagnosticTemplate]
    [UsedImplicitly]
    internal static IDiagnosticTemplate GameVersionOlderThanModMinimumGameVersionTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 12))
        .WithTitle("Game Version newer than supported by Mod")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("The minimum supported game version of {ModName} is {MinimumGameVersion}")
        .WithDetails("""
Mod {Mod} requires the game version to be at least {MinimumGameVersion}.
The current game version is {CurrentGameVersion}.

You can solve this issue by either updating your game or download an older version
of the mod from {NexusModsLink}.
""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<ModReference>("Mod")
            .AddValue<string>("ModName")
            .AddValue<string>("MinimumGameVersion")
            .AddValue<string>("CurrentGameVersion")
            .AddValue<NamedLink>("NexusModsLink")
        )
        .Finish();

    [DiagnosticTemplate]
    [UsedImplicitly]
    internal static IDiagnosticTemplate ModOverwritesGameFilesTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 13))
        .WithTitle("Mod overwrites game files")
        .WithSeverity(DiagnosticSeverity.Suggestion)
        .WithSummary("Mod {ModName} overwrites game files")
        .WithDetails("""
Mod {Mod} overwrites game files. This can cause compatibility issues and have other
unintended side-effects. See the {SMAPIWikiLink} for details.

You can resolve this diagnostic by replacing {Mod} with a SMAPI mod that doesn't
overwrite game files. See the {SMAPIWikiTableLink} for a list of alternatives.
""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<ModReference>("Mod")
            .AddValue<string>("ModName")
            .AddValue<NamedLink>("SMAPIWikiLink")
            .AddValue<NamedLink>("SMAPIWikiTableLink")
        )
        .Finish();
}
