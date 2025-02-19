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
        .WithTitle("Missing Dependency")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("'{SMAPIMod}' requires '{MissingDependencyModName}' which is not installed")
        .WithDetails("""
The mod **{SMAPIMod}** requires **{MissingDependencyModName}** to function, but **{MissingDependencyModName}** is not installed.


### How to Resolve
1. Download **{MissingDependencyModName}** from {NexusModsDependencyUri}
2. Add **{MissingDependencyModName}** to the loadout. 

### Technical Details
The `manifest.json` file included with **{SMAPIMod}** lists a mod with the ID `{MissingDependencyModId}` as a requirement or is using it as a framework in order function. 

The issue can arise in these scenarios:

1. **Missing Installation**: The required mod is not installed
2. **Incorrect Mod ID**: The manifest data for **{SMAPIMod}** might be incorrect


""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<LoadoutItemGroupReference>("SMAPIMod")
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
        .WithTitle("Outdated Dependency")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("'{Dependent}' requires an updated version of '{Dependency}'")
        .WithDetails("""
The mod **{Dependent}** requires **{Dependency}** version {MinimumVersion} or higher to function, but an older version of **{Dependency}** ({CurrentVersion}) is installed.

### How to Resolve
1. Download the latest version of **{Dependency}** from {NexusModsDependencyUri}
2. Add the latest version of **{Dependency}** to the loadout
3. Remove version {CurrentVersion} of **{Dependency}** from the loadout

### Technical Details
The `manifest.json` file included with **{Dependent}** lists **{Dependency}** as a requirement with a minimum version of {MinimumVersion}. 

"""
        )
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<LoadoutItemGroupReference>("Dependent")
            .AddDataReference<LoadoutItemGroupReference>("Dependency")
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
        .WithSummary("Stardew Modding API (SMAPI) is required for {ModCount} mod(s) but is not installed")
        .WithDetails("""
Stardew Modding API (SMAPI) is the mod loader required to run mods for Stardew Valley. The loadout contains {ModCount} mod(s) that require SMAPI to work, but it is not installed.

### How to Resolve
1. Download Stardew Modding API (SMAPI) from {NexusModsSMAPIUri}
2. Add SMAPI to the loadout

### Technical Details
Stardew Modding API (SMAPI) is required for most types of Stardew Valley mod as it provides core features that allow mod content to be loaded into the game.
"""
    )
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
        .WithSummary("Stardew Modding API (SMAPI) is required for {ModCount} mod(s) but it's not enabled")
        .WithDetails("""
Stardew Modding API (SMAPI) is the mod loader required to run mods for Stardew Valley. The loadout contains {ModCount} mod(s) that require SMAPI to work, but it is not enabled.

### How to Resolve
1. Enable SMAPI in "Installed Mods".

### Technical Details
Stardew Modding API (SMAPI) is required for most types of Stardew Valley mod as it provides core features that allow mod content to be loaded into the game.    
"""
    )
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<int>("ModCount")
        )
        .Finish();

    [DiagnosticTemplate]
    [UsedImplicitly]
    internal static IDiagnosticTemplate DisabledRequiredDependencyTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 5))
        .WithTitle("Disabled Dependency")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("'{SMAPIMod}' requires '{Dependency}' but it is disabled")
        .WithDetails("""
The mod **{SMAPIMod}** requires **{Dependency}** to function, but **{Dependency}** is not enabled.


### How to Resolve
1. Enable **{Dependency}** in "Installed Mods"

### Technical Details
The `manifest.json` file included with **{SMAPIMod}** lists **{Dependency}** as a requirement in order function. 

The issue can arise in these scenarios:

1. **Disabled Mod**: The required mod is disabled in the loadout
2. **Incorrect Mod ID**: The manifest data for **{SMAPIMod}** might be incorrect

"""
    )
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<LoadoutItemGroupReference>("SMAPIMod")
            .AddDataReference<LoadoutItemGroupReference>("Dependency")
        )
        .Finish();

    [DiagnosticTemplate]
    [UsedImplicitly]
    internal static IDiagnosticTemplate ModCompatabilityObsoleteTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 6))
        .WithTitle("Obsolete Mod")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("'{SMAPIModName}' is reported as obsolete by SMAPI")
        .WithDetails("""
The Stardew Modding API (SMAPI) mod compatibility list reports **{SMAPIMod}** as obsolete. This information is sourced from a combination of automated and community-submitted reports. 

### How to Resolve
1. Remove **{SMAPIMod}** from the loadout

### Technical Details
The Stardew Modding API (SMAPI) mod compatibility list has given the following information about the broken state of **{SMAPIMod}**:

> {SMAPIModName} is obsolete because {ReasonPhrase}

You may be able to find further information about this on the [SMAPI website](https://smapi.io/mods).

""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<LoadoutItemGroupReference>("SMAPIMod")
            .AddValue<string>("SMAPIModName")
            .AddValue<string>("ReasonPhrase")
        )
        .Finish();

    [DiagnosticTemplate]
    [UsedImplicitly]
    internal static IDiagnosticTemplate ModCompatabilityAssumeBrokenTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 7))
        .WithTitle("Broken Mod")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("'{SMAPIModName}' is reported as broken by SMAPI")
        .WithDetails("""
The Stardew Modding API (SMAPI) mod compatibility list reports **{SMAPIMod}** as broken. This information is sourced from a combination of automated and community-submitted reports. 

### How to Resolve
1. Check for a version of **{SMAPIMod}** newer than {ModVersion} on {ModLink}

OR

1. Remove **{SMAPIMod}** from the loadout

### Technical Details
The Stardew Modding API (SMAPI) mod compatibility list has given the following information about the broken state of **{SMAPIMod}**:

> {ReasonPhrase}

You may be able to find further information about this on the [SMAPI website](https://smapi.io/mods).
""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<LoadoutItemGroupReference>("SMAPIMod")
            .AddValue<string>("SMAPIModName")
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
            .AddDataReference<LoadoutItemGroupReference>("SMAPI")
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
            .AddDataReference<LoadoutItemGroupReference>("SMAPI")
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
        .WithTitle("Stardew Modding API (SMAPI) is not installed")
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
        .WithSummary("The minimum supported SMAPI version of {SMAPIModName} is {MinimumAPIVersion}")
        .WithDetails("""
Mod {SMAPIMod} requires the SMAPI version to be at least {MinimumAPIVersion}.
The current SMAPI version is {CurrentSMAPIVersion}.

You can solve this issue by either updating SMAPI or download an older version
of the mod from {NexusModsLink}. The latest SMAPI version can be downloaded
from {SMAPINexusModsLink}.
""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<LoadoutItemGroupReference>("SMAPIMod")
            .AddValue<string>("SMAPIModName")
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
        .WithSummary("The minimum supported game version of {SMAPIModName} is {MinimumGameVersion}")
        .WithDetails("""
Mod {SMAPIMod} requires the game version to be at least {MinimumGameVersion}.
The current game version is {CurrentGameVersion}.

You can solve this issue by either updating your game or download an older version
of the mod from {NexusModsLink}.
""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<LoadoutItemGroupReference>("SMAPIMod")
            .AddValue<string>("SMAPIModName")
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
        .WithSummary("Mod {GroupName} overwrites game files")
        .WithDetails("""
Mod {GroupName} overwrites game files. This can cause compatibility issues and have other
unintended side-effects. See the {SMAPIWikiLink} for details.

You can resolve this diagnostic by replacing {Group} with a SMAPI mod that doesn't
overwrite game files. See the {SMAPIWikiTableLink} for a list of alternatives.
""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<LoadoutItemGroupReference>("Group")
            .AddValue<string>("GroupName")
            .AddValue<NamedLink>("SMAPIWikiLink")
            .AddValue<NamedLink>("SMAPIWikiTableLink")
        )
        .Finish();
}
