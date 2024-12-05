using JetBrains.Annotations;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Generators.Diagnostics;
using NexusMods.Paths;

namespace NexusMods.Games.UnrealEngine;

internal static partial class Diagnostics
{
    private const string Source = "NexusMods.Games.UnrealEngine";

    [DiagnosticTemplate]
    [UsedImplicitly]
    internal static IDiagnosticTemplate UEAssetConflictTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 1))
        .WithTitle("Asset Conflict")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("Mods {ConflictingItems} are modifying the same asset '{ModifiedUEAsset}'")
        .WithDetails("""
Check that mods aren't mutually exclusive, otherwise disable all but one of them.
""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<string>("ConflictingItems")
            .AddValue<string>("ModifiedUEAsset")
        )
        .Finish();
}
