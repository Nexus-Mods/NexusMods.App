using NexusMods.DataModel.ModLists.Markers;
using NexusMods.Paths;

namespace NexusMods.CLI.Verbs;

public class InstallMod
{
    private readonly IRenderer _renderer;
    public InstallMod(Configurator configurator)
    {
        _renderer = configurator.Renderer;
    }
    
    public static VerbDefinition Definition = new("install-mod", "Installs a mod into a mod list", new OptionDefinition[]
    {
        new OptionDefinition<ModListMarker>("m", "modList", "Mod List to add the mod to"),
        new OptionDefinition<AbsolutePath>("f", "file", "Mod file to install")
    });


    public async Task Run(ModListMarker modList, AbsolutePath file, CancellationToken token)
    {
        await _renderer.WithProgress(token, async () =>
        {
            await modList.Install(file, token);
            return file;
        });
    }
}