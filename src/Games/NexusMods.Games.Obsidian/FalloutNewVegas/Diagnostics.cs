using JetBrains.Annotations;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Generators.Diagnostics;
using NexusMods.Paths;

namespace NexusMods.Games.Obsidian.FalloutNewVegas;

public static partial class Diagnostics
{
    public const string Source = "NexusMods.Games.Obsidian.FalloutNewVegas";

    [DiagnosticTemplate]
    [UsedImplicitly]
    internal static IDiagnosticTemplate MissingNVSE = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 2))
        .WithTitle("Missing NVSE")
        .WithSeverity(DiagnosticSeverity.Suggestion)
        .WithSummary("NVSE is not installed")
        .WithDetails("""
The NVSE (New Vegas Script Extender) may be required for mods to function properly, but it is not installed. You can download the latest 
version of xNVSE from the Nexus website {xNVSELink}.
""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<NamedLink>("xNVSELink")
        )
        .Finish();
}
