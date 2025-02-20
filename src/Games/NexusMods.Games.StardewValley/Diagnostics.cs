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
1. Download the latest version of **{Dependency}** (version {MinimumVersion} or newer) from {NexusModsDependencyUri}
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

You may be able to find further information about this on the {SMAPIModList}.

""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<LoadoutItemGroupReference>("SMAPIMod")
            .AddValue<string>("SMAPIModName")
            .AddValue<string>("ReasonPhrase")
            .AddValue<NamedLink>("SMAPIModList")
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
        .WithTitle("Game Update required")
        .WithSeverity(DiagnosticSeverity.Critical)
        .WithSummary("Stardew Modding API (SMAPI) {SMAPIVersion} requires Stardew Valley {MinimumGameVersion}+")
        .WithDetails("""
The installed version of Stardew Modding API (SMAPI) will not work correctly for game versions older than {MinimumGameVersion}. The current game version is {CurrentGameVersion}.

The game may crash or fail to launch if this issue remains unresolved.

### How to Resolve
1. Update Stardew Valley to {MinimumGameVersion} or higher

OR

1. Download SMAPI version {NewestSupportedSMAPIVersionForCurrentGameVersion} from {SMAPINexusModsLink}

### Technical Details
Stardew Valley version {SMAPIVersion} is listed as requiring Stardew Valley {MinimumGameVersion} or newer in the compatibility data on {GitHubData}.

""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<LoadoutItemGroupReference>("SMAPI")
            .AddValue<string>("SMAPIVersion")
            .AddValue<string>("MinimumGameVersion")
            .AddValue<string>("CurrentGameVersion")
            .AddValue<string>("NewestSupportedSMAPIVersionForCurrentGameVersion")
            .AddValue<NamedLink>("SMAPINexusModsLink")
            .AddValue<NamedLink>("GitHubData")
        )
        .Finish();

    [DiagnosticTemplate]
    [UsedImplicitly]
    internal static IDiagnosticTemplate GameVersionNewerThanMaximumGameVersionTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 9))
        .WithTitle("SMAPI Update Required")
        .WithSeverity(DiagnosticSeverity.Critical)
        .WithSummary("Stardew Modding API (SMAPI) {SMAPIVersion} requires Stardew Valley {MaximumGameVersion} or older")
        .WithDetails("""
The installed version of Stardew Modding API (SMAPI) will not work correctly for game versions newer than {MaximumGameVersion}. The current game version is {CurrentGameVersion}.

The game may crash or fail to launch if this issue remains unresolved.

### How to Resolve
1. Download the latest version of **SMAPI** ({NewestSupportedSMAPIVersionForCurrentGameVersion}) from {SMAPINexusModsLink}

OR

1. Downgrade your installation of Stardew Valley to version {MaximumGameVersion}

### Technical Details
SMAPI {SMAPIVersion} is listed as requiring a maximum Stardew Valley version of {MaximumGameVersion} in the compatibility data on {GitHubData}.
""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<LoadoutItemGroupReference>("SMAPI")
            .AddValue<string>("SMAPIVersion")
            .AddValue<string>("MaximumGameVersion")
            .AddValue<string>("CurrentGameVersion")
            .AddValue<string>("NewestSupportedSMAPIVersionForCurrentGameVersion")
            .AddValue<NamedLink>("SMAPINexusModsLink")
            .AddValue<NamedLink>("GitHubData")
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
        .WithTitle("SMAPI Update Required")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("'{SMAPIModName}' requires SMAPI version {MinimumAPIVersion}+")
        .WithDetails("""
The mod **{SMAPIMod}** requires the Stardew Modding API (SMAPI) version {MinimumAPIVersion} or higher to function. 
The current SMAPI version is {CurrentSMAPIVersion}.

### How to Resolve
1. Download the latest version of SMAPI ({CurrentSMAPIVersion}) from {SMAPINexusModsLink}

OR

1. Download an older version of **{SMAPIMod}** from {NexusModsLink} that works with SMAPI version {CurrentSMAPIVersion}

### Technical Details
The `manifest.json` file included with **{SMAPIMod}** lists the `MinimumApiVersion` as {MinimumAPIVersion}. 
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
        .WithTitle("Some Mods Require a Game Update")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("'{SMAPIModName}' requires Stardew Valley {MinimumGameVersion}+")
        .WithDetails("""
The mod **{SMAPIMod}** requires the Stardew Valley version {MinimumGameVersion} or higher to function. The current game version is {CurrentGameVersion}.

### How to Resolve
1. Update Stardew Valley to {MinimumGameVersion} or higher

OR

1. Download an older version of **{SMAPIModName}** from {NexusModsLink} that works with game version {CurrentGameVersion}

### Technical Details
The `manifest.json` file included with **{SMAPIModName}** lists the `MinimumGameVersion` as {MinimumGameVersion}.
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
        .WithTitle("Overwritten Game Files")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("'{GroupName}' overwrites game files directly which can cause issues")
        .WithDetails("""
The mod **{GroupName}** appears to be an "XNB mod" which overwrites game files directly rather than using a content patcher. 

### How to Resolve
1. Check the {SMAPIWikiTableLink} for SMAPI or Content Patcher alternatives to **{GroupName}**

OR

1. Remove **{GroupName}** from the loadout


### Why are XNB mods discouraged?
XNB mods have a lot of limitations. They often conflict with each other, usually break when the game updates, and in rare cases can even corrupt your saved games. You can learn more about XNB mods on the {SMAPIWikiLink}.
""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<LoadoutItemGroupReference>("Group")
            .AddValue<string>("GroupName")
            .AddValue<NamedLink>("SMAPIWikiLink")
            .AddValue<NamedLink>("SMAPIWikiTableLink")
        )
        .Finish();
}
