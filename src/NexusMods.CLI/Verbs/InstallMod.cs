using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.Paths;

namespace NexusMods.CLI.Verbs;

// ReSharper disable once ClassNeverInstantiated.Global
public class InstallMod : AVerb<LoadoutMarker, AbsolutePath, string>
{
    private readonly IRenderer _renderer;
    public InstallMod(Configurator configurator) => _renderer = configurator.Renderer;

    public static VerbDefinition Definition => new("install-mod", "Installs a mod into a loadout", new OptionDefinition[]
    {
        new OptionDefinition<LoadoutMarker>("l", "loadout", "loadout to add the mod to"),
        new OptionDefinition<AbsolutePath>("f", "file", "Mod file to install"),
        new OptionDefinition<string>("n", "name", "Name of the mod after installing")
    });

    public async Task<int> Run(LoadoutMarker loadout, AbsolutePath file, string name, CancellationToken token)
    {
        await _renderer.WithProgress(token, async () =>
        {
            await loadout.Install(file, name, token);
            return file;
        });
        return 0;
    }
}
