using JetBrains.Annotations;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Games.RedEngine.Cyberpunk2077.Emitters;
using NexusMods.Generators.Diagnostics;

namespace NexusMods.Games.RedEngine.Cyberpunk2077;

public static partial class Diagnostics
{
    public const string Source = "NexusMods.Games.RedEngine.Cyberpunk2077";

    [DiagnosticTemplate] 
    [UsedImplicitly] 
    internal static IDiagnosticTemplate MissingModWithKnownNexusUri = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 1))
        .WithTitle("{DependencyName} is missing")
        .WithSeverity(DiagnosticSeverity.Critical)
        .WithSummary("The mod '{Mod}' requires {DependencyName} to function properly, but it is missing.")
        .WithDetails("""
Either {DependencyName} is not installed or has been disabled.

You can download the latest version of `{DependencyName}` from {NexusModsDependencyUri}.
""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<ModReference>("Mod")
            .AddValue<string>("DependencyName")
            .AddValue<NamedLink>("NexusModsDependencyUri")
        )
        .Finish();
}
