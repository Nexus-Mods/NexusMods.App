using JetBrains.Annotations;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.References;
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
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("Mod {Mod} is missing required dependency {MissingDependency}")
        .WithoutDetails()
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<ModReference>("Mod")
            .AddValue<string>("MissingDependency")
        )
        .Finish();

    [DiagnosticTemplate]
    [UsedImplicitly]
    internal static IDiagnosticTemplate OutdatedDependencyTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 2))
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("Mod {Dependent} requires at least version {MinimumVersion} of {Dependency} but installed is {CurrentVersion}")
        .WithoutDetails()
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<ModReference>("Dependent")
            .AddDataReference<ModReference>("Dependency")
            .AddValue<string>("MinimumVersion")
            .AddValue<string>("CurrentVersion")
        )
        .Finish();

    [DiagnosticTemplate]
    [UsedImplicitly]
    internal static IDiagnosticTemplate SMAPIRequiredButNotInstalledTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 3))
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("SMAPI is required for {ModCount} Mod(s) but it's not installed")
        .WithDetails("You can install the latest SMAPI version at {NexusModsSMAPIUri}.")
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<int>("ModCount")
            .AddValue<Uri>("NexusModsSMAPIUri")
        )
        .Finish();

    [DiagnosticTemplate]
    [UsedImplicitly]
    internal static IDiagnosticTemplate SMAPIRequiredButDisabledTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 4))
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("SMAPI is required for {ModCount} Mod(s) but it's not enabled")
        .WithoutDetails()
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<int>("ModCount")
        )
        .Finish();
}
