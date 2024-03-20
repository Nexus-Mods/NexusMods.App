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
}
