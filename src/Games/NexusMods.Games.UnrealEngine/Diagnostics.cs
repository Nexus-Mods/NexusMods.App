using JetBrains.Annotations;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Generators.Diagnostics;
using NexusMods.Paths;

namespace NexusMods.Games.UnrealEngine;

internal static partial class Diagnostics
{
    private const string Source = "NexusMods.Games.UnrealEngine";

    [DiagnosticTemplate]
    [UsedImplicitly]
    internal static IDiagnosticTemplate UnrealEngineAssetConflictTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 1))
        .WithTitle("Asset Conflict")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("Mods {ConflictingItems} are modifying the same asset '{ModifiedUEAsset}'")
        .WithDetails("""
Check that mods aren't mutually exclusive, otherwise disable all but one of them.
""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<string>("ConflictingItems")
            .AddValue<string>("ModifiedUEAsset")
        )
        .Finish();
    
    [DiagnosticTemplate]
    [UsedImplicitly]
    internal static IDiagnosticTemplate ModOverwritesGameFilesTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 2))
        .WithTitle("Mod overwrites game files")
        .WithSeverity(DiagnosticSeverity.Suggestion)
        .WithSummary("Mod {GroupName} overwrites game files")
        .WithDetails("""
Mod {GroupName} overwrites game files. This can cause compatibility issues and have other
unintended side-effects.

You can resolve this diagnostic by replacing {Group} with a different mod which doesn't
overwrite game files.
""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<LoadoutItemGroupReference>("Group")
            .AddValue<string>("GroupName")
        )
        .Finish();
    
    [DiagnosticTemplate]
    [UsedImplicitly]
    internal static IDiagnosticTemplate ScriptingSystemRequiredButNotInstalledTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 3))
        .WithTitle("UE4SS is not installed")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("UE4SS is required for {ModCount} Mod(s) but it's not installed")
        .WithDetails("You can download the latest UE4SS version from {NexusModsUE4SSUri}.")
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<int>("ModCount")
            .AddValue<NamedLink>("NexusModsUE4SSUri")
        )
        .Finish();

    [DiagnosticTemplate]
    [UsedImplicitly]
    internal static IDiagnosticTemplate ScriptingSystemRequiredButDisabledTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId(Source, number: 4))
        .WithTitle("UE4SS is not enabled")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("UE4SS is required for {ModCount} Mod(s) but it's not enabled")
        .WithoutDetails()
        .WithMessageData(messageBuilder => messageBuilder
            .AddValue<int>("ModCount")
        )
        .Finish();
}
