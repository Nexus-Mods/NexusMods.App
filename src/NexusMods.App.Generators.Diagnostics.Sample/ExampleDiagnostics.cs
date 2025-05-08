using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Generators.Diagnostics;

namespace NexusMods.App.Generators.Diagnostics.Sample;

public partial class ExampleDiagnostics
{
    [DiagnosticTemplate]
    private static readonly IDiagnosticTemplate Diagnostic1Template = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(source: "Example", number: 1))
        .WithTitle("Diagnostic 1")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("Mod conflicts because it's missing '{Something}'!")
        .WithoutDetails()
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<string>("Something")
        )
        .Finish();
}
