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
}
