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
        // Red4ExtMissingDiagnosticEmitter.Id
        .WithId(new DiagnosticId(Source, number: 1))
        .WithTitle("Red4Ext is missing")
        .WithSeverity(DiagnosticSeverity.Critical)
        .WithSummary("The mod '{Mod}' requires {DependencyName} to function properly, but it is missing.")
        .WithDetails("""
Either {DependencyName} is not installed or has been disabled. We've detected that  

You can download the latest version of `{DependencyName}` from [Nexus Mods]({NexusModsDependencyUri}).
""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<ModReference>("Mod")
            .AddValue<string>("DependencyName")
            .AddValue<NamedLink>("NexusModsDependencyUri")
        )
        .Finish();

    [DiagnosticTemplate] 
    [UsedImplicitly] 
    internal static IDiagnosticTemplate CyberEngineTweaksMissing = DiagnosticTemplateBuilder
        .Start()
        // CyberEngineTweaksMissingDiagnosticEmitter.Id
        .WithId(new DiagnosticId(Source, number: 2))
        .WithTitle("Red4Ext is missing")
        .WithSeverity(DiagnosticSeverity.Critical)
        .WithSummary("A installed mod requires {1} to function properly, but it is missing.")
        .WithDetails("The mod '{0}' requires Red4Ext to function properly, but either {1} is not installed or the mod has been disabled.")
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<ModReference>("Mod")
            .AddValue<NamedLink>("Red4ExtDownloadLink")
        )
        .Finish();
}
