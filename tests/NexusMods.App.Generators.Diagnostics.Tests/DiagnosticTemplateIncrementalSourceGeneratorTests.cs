using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NexusMods.Abstractions.Diagnostics;

namespace NexusMods.App.Generators.Diagnostics.Tests;

public class DiagnosticTemplateIncrementalSourceGeneratorTests
{
    [LanguageInjection("csharp")]
    private const string SourceText = """
using NexusMods.Generators.Diagnostics;
using NexusMods.Abstractions.Diagnostic;
using NexusMods.Abstractions.Diagnostics.References;

namespace TestNamespace;

internal partial class MyClass
{
    private const string Source = "Example";

    [DiagnosticTemplate]
    private static readonly IDiagnosticTemplate Diagnostic1Template = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(source: Source, number: 1))
        .WithTitle("Diagnostic 1")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("Mod '{ModA}' conflicts with '{ModB}' because it's missing '{Something}' and {Count} other stuff!")
        .WithoutDetails()
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<ModReference>("ModA")
            .AddDataReference<ModReference>("ModB")
            .AddValue<string>("Something")
            .AddValue<int>("Count")
        )
        .Finish();
}
""";

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
