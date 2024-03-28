using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Generators.Diagnostics;

namespace Examples.Diagnostics;

internal static partial class Diagnostics
{
    [DiagnosticTemplate]
    [UsedImplicitly]
    internal static IDiagnosticTemplate ModCompatabilityObsoleteTemplate = DiagnosticTemplateBuilder
        .Start()
        .WithId(new DiagnosticId("Examples", number: 6))
        .WithTitle("Mod is obsolete")
        .WithSeverity(DiagnosticSeverity.Warning)
        .WithSummary("Mod {Mod} is obsolete")
        .WithDetails("""
Mod {Mod} has been made obsolete:

> {ModName} is obsolete because {ReasonPhrase}
""")
        .WithMessageData(messageBuilder => messageBuilder
            .AddDataReference<ModReference>("Mod")
            .AddValue<string>("ModName")
            .AddValue<string>("ReasonPhrase")
        )
        .Finish();
}

file class MyDiagnosticLoadoutEmitter : ILoadoutDiagnosticEmitter
{
    public IAsyncEnumerable<Diagnostic> Diagnose(
        Loadout loadout,
        CancellationToken cancellationToken)
    {
        var res = new List<Diagnostic>();

        var someMod = loadout.Mods.First().Value;

        // this "Create" method was generated for you
        res.Add(Diagnostics.CreateModCompatabilityObsolete(
                Mod: someMod.ToReference(loadout),
                ModName: someMod.Name,
                ReasonPhrase: "it's incompatible"
            )
        );

        // alternatively, use the "yield"/generator pattern
        return res.ToAsyncEnumerable();
    }
}
