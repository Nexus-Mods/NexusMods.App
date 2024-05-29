using System.Runtime.CompilerServices;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;

namespace NexusMods.Games.StardewValley.Emitters;

public class ModOverwritesGameFilesEmitter : ILoadoutDiagnosticEmitter
{
    private static readonly NamedLink SMAPIWikiLink = new("SMAPI Wiki", new Uri("https://stardewvalleywiki.com/Modding:Using_XNB_mods"));
    private static readonly NamedLink SMAPIWikiTableLink = new("SMAPI Wiki", new Uri("https://stardewvalleywiki.com/Modding:Using_XNB_mods#Alternatives_using_Content_Patcher"));

    private static readonly GamePath ContentDirectoryPath = new(LocationId.Game, "Content");

    public async IAsyncEnumerable<Diagnostic> Diagnose(Loadout.Model loadout, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();

        var mods = loadout
            .GetEnabledMods()
            .Where(mod => mod.Files.Any(file => file.To.StartsWith(ContentDirectoryPath)))
            .ToArray();

        foreach (var mod in mods)
        {
            yield return Diagnostics.CreateModOverwritesGameFiles(
                Mod: mod.ToReference(loadout),
                ModName: mod.Name,
                SMAPIWikiLink: SMAPIWikiLink,
                SMAPIWikiTableLink: SMAPIWikiTableLink
            );
        }
    }
}
