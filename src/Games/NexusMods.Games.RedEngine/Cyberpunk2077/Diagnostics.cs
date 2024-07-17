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
        .WithTitle("Missing Mod with Known Nexus URI")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("The mod '{Mod}' requires {DependencyName} to function properly, but it is missing.")
        .WithDetails("""

You can download the latest version of `{DependencyName}` from {NexusModsDependencyUri}.
""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<ModReference>("Mod")
            .AddValue<string>("DependencyName")
            .AddValue<NamedLink>("NexusModsDependencyUri")
        )
        .Finish();
    
    [DiagnosticTemplate] 
    [UsedImplicitly] 
    internal static IDiagnosticTemplate DisabledModDependency = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 2))
        .WithTitle("Disabled Mod Dependency")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("The mod '{Mod}' requires '{DependencyMod}' to function properly, but it is disabled.")
        .WithDetails("Please re-enable '{DependencyMod}' to ensure '{Mod}' functions properly.")
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<ModReference>("Mod")
            .AddDataReference<ModReference>("DependencyMod")
        )
        .Finish();
}
