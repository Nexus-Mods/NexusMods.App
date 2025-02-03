using JetBrains.Annotations;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Generators.Diagnostics;
using NexusMods.Paths;

namespace NexusMods.Games.FileHashes;

public partial class Diagnostics
{
    private const string Source = "NexusMods.Games.FileHashes";

    [DiagnosticTemplate] 
    [UsedImplicitly] 
    internal static IDiagnosticTemplate GameFilesDoNotHaveSource = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 1))
        .WithTitle("Game files are not backed up")
        .WithSeverity(DiagnosticSeverity.Suggestion)
        .WithSummary("There are {FileCount} game files that do not have a backed up source")
        .WithDetails("""
We've detected that there are {FileCount} game files, totaling {Size}, that do not have a backed up source. This is not inherently a problem, and we will back up any files we need to replace. But any
changes made by you or a game store (such as GOG Galaxy, Steam, etc.) may render this loadout inoperable if these files are not first backed up. 
""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<Size>("Size")
            .AddValue<int>("FileCount")
        )
        .Finish();
    
    [DiagnosticTemplate] 
    [UsedImplicitly] 
    internal static IDiagnosticTemplate UndeployableLoadoutDueToMissingGameFiles = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 1))
        .WithTitle("Loadout is undeployable due to missing game files")
        .WithSeverity(DiagnosticSeverity.Critical)
        .WithSummary("There are {FileCount} game files that do not have a valid source")
        .WithDetails("""
We've detected that there are {FileCount} game files, totaling {Size}, that do not have a valid source. However, in order to deploy the current loadout these files are needed. Until the loadout
is updated to not include these files or the files can be sourced, the loadout is undeployable."
""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<Size>("Size")
            .AddValue<int>("FileCount")
        )
        .Finish();
}
