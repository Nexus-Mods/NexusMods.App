using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger _logger;
    private static readonly NamedLink SMAPIWikiLink = new("SMAPI Wiki", new Uri("https://stardewvalleywiki.com/Modding:Using_XNB_mods"));
    private static readonly NamedLink SMAPIWikiTableLink = new("SMAPI Wiki", new Uri("https://stardewvalleywiki.com/Modding:Using_XNB_mods#Alternatives_using_Content_Patcher"));

    private static readonly GamePath ContentDirectoryPath = new(LocationId.Game, Constants.ContentFolder);

    public ModOverwritesGameFilesEmitter(
        ILogger<ModOverwritesGameFilesEmitter> logger
        )
    {
        _logger = logger;
    }

    public async IAsyncEnumerable<Diagnostic> Diagnose(
        Loadout.ReadOnly loadout,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();

        IEnumerable<LoadoutItemGroup.ReadOnly> groups;
        try
        {
            groups = loadout.Items
            .GetEnabledLoadoutFiles()
            .Where(file =>
            {
                var loadoutItem = file.AsLoadoutItemWithTargetPath().AsLoadoutItem();
                if (loadoutItem.ParentId == default(LoadoutItemGroupId)) return false;
                return !loadoutItem.Parent.TryGetAsLoadoutGameFilesGroup(out _);
            })
            .Where(file => ((GamePath)file.AsLoadoutItemWithTargetPath().TargetPath).StartsWith(ContentDirectoryPath))
            .Select(file => file.AsLoadoutItemWithTargetPath().AsLoadoutItem().Parent)
            .DistinctBy(item => item.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error checking for ModOverwrites: {error}", ex);
            yield break;
        } 

        foreach (var group in groups)
        {
            yield return Diagnostics.CreateModOverwritesGameFiles(
                Group: group.ToReference(loadout),
                GroupName: group.AsLoadoutItem().Name,
                SMAPIWikiLink: SMAPIWikiLink,
                SMAPIWikiTableLink: SMAPIWikiTableLink
            );
        }
    }
}
