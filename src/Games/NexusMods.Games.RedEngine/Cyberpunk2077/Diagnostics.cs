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
    internal static IDiagnosticTemplate Red4ExtMissing = DiagnosticTemplateBuilder
        .Start()
        // Red4ExtMissingDiagnosticEmitter.Id
        .WithId(new DiagnosticId(Source, number: 1))
        .WithTitle("Red4Ext is missing")
        .WithSeverity(DiagnosticSeverity.Critical)
        .WithSummary("A installed mod requires Red4Ext to function properly, but it is missing.")
        .WithDetails("The mod '{0}' requires Red4Ext to function properly, but either Read4Ext is not installed or the mod has been disabled.")
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<ModReference>("Mod")
            .AddValue<NamedLink>("Red4ExtDownloadLink")
        )
        .Finish();
}
