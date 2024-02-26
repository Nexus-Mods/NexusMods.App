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
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("Mod '{ModA}' conflicts with '{ModB}'!")
        .WithoutDetails()
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<ModReference>("ModA")
            .AddDataReference<ModReference>("ModB")
        )
        .Finish();
}
