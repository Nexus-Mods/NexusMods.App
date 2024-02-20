using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NexusMods.Abstractions.Diagnostics;

namespace NexusMods.App.Generators.Diagnostics.Tests;

public class DiagnosticTemplateIncrementalSourceGeneratorTests
{
    private const string SourceText = @"
using NexusMods.Generators.Diagnostics;
using NexusMods.Abstractions.Diagnostic;
using NexusMods.Abstractions.Diagnostics.References;

namespace TestNamespace;

internal partial class MyClass
{
    [DiagnosticTemplate]
    private static readonly IDiagnosticTemplate Diagnostic1Template = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(source: ""MyCoolSource"", number: 13))
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithMessage(""Mod '{ModA}' is not working because of '{ModB}'!"", messageBuilder => messageBuilder
            .AddDataReference<ModReference>(""ModA"")
            .AddDataReference<ModReference>(""ModB"")
        )
        .Finish();
}";

    [Fact]
    public Task TestGenerator()
    {
        var generator = new DiagnosticTemplateIncrementalSourceGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        var compilation = CSharpCompilation.Create(nameof(DiagnosticTemplateIncrementalSourceGenerator),
            new[] { CSharpSyntaxTree.ParseText(SourceText) },
            new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(DiagnosticTemplateBuilder).Assembly.Location),
            }
        );

        var runResult = driver.RunGenerators(compilation).GetRunResult();
        return Verify(runResult);
    }
}
