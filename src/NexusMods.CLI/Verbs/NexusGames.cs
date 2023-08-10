using NexusMods.Abstractions.CLI;
using NexusMods.Abstractions.CLI.DataOutputs;
using NexusMods.Networking.NexusWebApi;

// Temporary until moved to CLI project.
#pragma warning disable CS1591
namespace NexusMods.CLI.Verbs;

public class NexusGames : AVerb, IRenderingVerb
{
    private readonly Client _client;

    /// <inheritdoc />
    public IRenderer Renderer { get; set; } = null!;

    public NexusGames(Client client)
    {
        _client = client;
    }
    public static VerbDefinition Definition => new("nexus-games", "Lists all games available on Nexus Mods",
        Array.Empty<OptionDefinition>());



    public async Task<int> Run(CancellationToken token)
    {
        var results = await _client.Games(token);

        await Renderer.Render(new Table(new[] { "Name", "Domain", "Downloads", "Files" },
            results.Data
                .OrderByDescending(x => x.FileCount)
                .Select(x => new object[] { x.Name, x.DomainName, x.Downloads, x.FileCount })));

        return 0;
    }
}
