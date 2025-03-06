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
        .WithTitle("Missing Game Files")
        .WithSeverity(DiagnosticSeverity.Critical)
        .WithSummary("Loadout can't be applied due to {FileCount} missing game file(s) with no valid source")
        .WithDetails("""
The loadout is based on {Game} v{Version} but there are {FileCount} file(s) missing from the game installation. Unless these file(s) are restored, the loadout cannot be applied. 
 
## How to Resolve
1. Open the {Store} launcher 
2. Verify or repair the game files
3. Close and reopen the app

## Technical Details
While checking the contents of the game folder against the file list index for {Game} (v{Version}) from {Store}, {FileCount} file(s) - totalling {Size} - could not be located in the game folder, have not been backed up by the app or cannot be fetched from {Store} automatically.

Without all the required base game files, the loadout cannot be applied.

""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<Size>("Size")
            .AddValue<int>("FileCount")
            .AddValue<string>("Game")
            .AddValue<string>("Store")
            .AddValue<string>("Version")
        )
        .Finish();
}
