using System.Text.RegularExpressions;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.Emitters;

/// <summary>
/// Virtual Atelier is a mod that adds a virtual shop to the game. It provides a redscript function that other mods can use to register their own
/// items with the shop. We scan .reds files for this function.
/// </summary>
/// <param name="store"></param>
public partial class VirtualAtelierDependencyMatcher(IFileStore store) : AFileStringContentsDependencyEmitter(store)
{
    public override Regex[] DependantRegexes { get; } = [RegistrationMatcher()];

    protected override string DependencyName => "Virtual Atelier";

    protected internal override GamePath[] DependantPaths { get; } =
    [
        new GamePath(LocationId.Game, "r6/scripts"),
    ];
    
    protected internal override Extension[] DependantExtensions { get; } =
    [
        new Extension(".reds"),
    ];

    protected internal override GamePath[] DependencyPaths { get; } =
    [
        new GamePath(LocationId.Game, "archive/pc/mod/VirtualAtelier.archive"),
        new GamePath(LocationId.Game, "archive/pc/mod/VirtualAtelier.archive.xl"),
        new GamePath(LocationId.Game, "r6/scripts/virtual-atelier-full/core/Events.reds")
    ];
    
    protected override GameDomain Domain => Cyberpunk2077Game.StaticDomain;
    protected override ModId ModId { get; } = ModId.From(2987);

    [GeneratedRegex("ref<VirtualShopRegistration>")]
    private static partial Regex RegistrationMatcher();
}
