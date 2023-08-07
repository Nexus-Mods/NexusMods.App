using NexusMods.Abstractions.CLI;
using NexusMods.CLI.DataOutputs;
using NexusMods.Networking.NexusWebApi;

// Temporary until moved to CLI project.
#pragma warning disable CS1591
namespace NexusMods.CLI.Verbs;

public class NexusApiVerify : AVerb
{
    public NexusApiVerify(Configurator configurator, Client client, IAuthenticatingMessageFactory messageFactory)
    {
        _client = client;
        _renderer = configurator.Renderer;
        _messageFactory = messageFactory;
    }

    public static VerbDefinition Definition => new("nexus-api-verify",
        "Verifies the logged in account via the Nexus API",
        Array.Empty<OptionDefinition>());

    private readonly Client _client;
    private readonly IRenderer _renderer;
    private readonly IAuthenticatingMessageFactory _messageFactory;

    public async Task<int> Run(CancellationToken token)
    {
        var userInfo = await _messageFactory.Verify(_client, token);
        await _renderer.Render(new Table(new[] { "Name", "Premium", "Supporter" },
            new[]
            {
                new object[]
                {
                    userInfo?.Name ?? "<Not logged in>",
                    userInfo?.IsPremium ?? false,
                    userInfo?.IsSupporter ?? false,
                }
            }));

        return 0;
    }
}
