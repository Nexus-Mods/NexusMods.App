using NexusMods.CLI.DataOutputs;
using NexusMods.DataModel.Games;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Networking.NexusWebApi.NMA.Extensions;
using NexusMods.Networking.NexusWebApi.Types;

// Temporary until moved to CLI project.
#pragma warning disable CS1591

namespace NexusMods.CLI.Verbs;

public class DownloadLinks : AVerb<GameDomain, ModId, FileId>
{
    private readonly Client _client;
    private readonly IRenderer _renderer;

    public DownloadLinks(Client client, Configurator configurator)
    {
        _client = client;
        _renderer = configurator.Renderer;
    }

    public static VerbDefinition Definition => new VerbDefinition("nexus-download-links",
        "Generates download links for a given file",
        new OptionDefinition[]
        {
            new OptionDefinition<GameDomain>("g", "gameDomain", "Game domain"),
            new OptionDefinition<ModId>("m", "modId", "Mod ID"),
            new OptionDefinition<FileId>("f", "fileId", "File ID"),
        });

    public async Task<int> Run(GameDomain gameDomain, ModId modId, FileId fileId, CancellationToken token)
    {
        var links = await _client.DownloadLinksAsync(gameDomain, modId, fileId, token);

        await _renderer.Render(new Table(new[] { "Source", "Link" },
            links.Data.Select(x => new object[] { x.ShortName, x.Uri })));
        return 0;
    }
}
