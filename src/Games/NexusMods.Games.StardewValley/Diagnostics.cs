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
        .WithSummary("Mod {Mod} is missing required dependency '{MissingDependency}'")
        .WithDetails("You can download the latest version at {NexusModsDependencyUri}.")
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<ModReference>("Mod")
            .AddValue<string>("MissingDependency")
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

The last supported SMAPI version for game version {CurrentGameVersion} is {LastSupportedSMAPIVersionForCurrentGameVersion}.
You can download this SMAPI version from {SMAPINexusModsLink}.
""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<ModReference>("SMAPIMod")
            .AddValue<string>("SMAPIVersion")
            .AddValue<string>("MinimumGameVersion")
            .AddValue<string>("CurrentGameVersion")
            .AddValue<string>("LastSupportedSMAPIVersionForCurrentGameVersion")
            .AddValue<NamedLink>("SMAPINexusModsLink")
        )
        .Finish();
}
