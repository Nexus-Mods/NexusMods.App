using NexusMods.Common;
using NexusMods.DataModel.Abstractions.Games;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Games.GameCapabilities.FolderMatchInstallerCapability;
using NexusMods.DataModel.ModInstallers;
using NexusMods.FileExtractor.StreamFactories;
using NexusMods.Games.BladeAndSorcery.Installers;
using NexusMods.Paths;

namespace NexusMods.Games.BladeAndSorcery;

public class BladeAndSorcery : AGame, ISteamGame
{
    private readonly IServiceProvider _serviceProvider;
    public IEnumerable<uint> SteamIds => new[] { 629730u };

    public BladeAndSorcery(IServiceProvider serviceProvider) : base(serviceProvider)
        => _serviceProvider = serviceProvider;

    public override string Name => "Blade & Sorcery";
    public override GameDomain Domain => GameDomain.From("bladeandsorcery");

    public override GamePath GetPrimaryFile(GameStore store) => new(LocationId.Game, "BladeAndSorcery.exe");

    protected override IReadOnlyDictionary<LocationId, AbsolutePath> GetLocations(IFileSystem fileSystem, GameLocatorResult installation)
    {
        var savesDirectory = fileSystem
            .GetKnownPath(KnownPath.MyGamesDirectory)
            .Combine("BladeAndSorcery/Saves/Default");

        var globalOptionsFile = savesDirectory
            .Combine("Options.opt");

        return new Dictionary<LocationId, AbsolutePath>
        {
            [LocationId.Game] = installation.Path,
            [LocationId.Preferences] = globalOptionsFile,
            [LocationId.Saves] = savesDirectory
        };
    }

    public override List<IModInstallDestination> GetInstallDestinations(IReadOnlyDictionary<LocationId, AbsolutePath> locations) =>
        ModInstallDestinationHelpers.GetCommonLocations(locations);

    public override IStreamFactory Icon =>
        new EmbededResourceStreamFactory<BladeAndSorcery>("NexusMods.Games.BladeAndSorcery.Resources.BladeAndSorcery.icon.jpg");

    public override IStreamFactory GameImage =>
        new EmbededResourceStreamFactory<BladeAndSorcery>("NexusMods.Games.BladeAndSorcery.Resources.BladeAndSorcery.game_image.jpg");

    /// <inheritdoc />
    public override IEnumerable<IModInstaller> Installers => new IModInstaller[]
    {
        BladeAndSorceryModInstaller.Create(_serviceProvider)
    };
}
