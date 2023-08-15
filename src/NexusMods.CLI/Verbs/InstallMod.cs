using NexusMods.Abstractions.CLI;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.Paths;

namespace NexusMods.CLI.Verbs;

// ReSharper disable once ClassNeverInstantiated.Global
/// <summary>
/// Installs a mod into a loadout
/// </summary>
public class InstallMod : AVerb<LoadoutMarker, AbsolutePath, string>, IRenderingVerb
{
    private readonly IArchiveInstaller _archiveInstaller;
    private readonly IArchiveAnalyzer _archiveAnalyzer;

    /// <inheritdoc />
    public IRenderer Renderer { get; set; } = null!;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="archiveInstaller"></param>
    /// <param name="archiveAnalyzer"></param>
    public InstallMod(IArchiveInstaller archiveInstaller, IArchiveAnalyzer archiveAnalyzer)
    {
        _archiveInstaller = archiveInstaller;
        _archiveAnalyzer = archiveAnalyzer;
    }

    /// <inheritdoc />
    public static VerbDefinition Definition => new("install-mod", "Installs a mod into a loadout", new OptionDefinition[]
    {
        new OptionDefinition<LoadoutMarker>("l", "loadout", "loadout to add the mod to"),
        new OptionDefinition<AbsolutePath>("f", "file", "Mod file to install"),
        new OptionDefinition<string>("n", "name", "Name of the mod after installing")
    });

    /// <inheritdoc />
    public async Task<int> Run(LoadoutMarker loadout, AbsolutePath file, string name, CancellationToken token)
    {
        await Renderer.WithProgress(token, async () =>
        {
            var analyzedFile = await _archiveAnalyzer.AnalyzeFileAsync(file, token);
            await _archiveInstaller.AddMods(loadout.Value.LoadoutId, analyzedFile.Hash, token:token);
            return file;
        });
        return 0;
    }
}
