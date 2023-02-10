using NexusMods.CLI;
using NexusMods.CLI.DataOutputs;

namespace NexusMods.Networking.NexusWebApi.Verbs;

public class NexusApiVerify : AVerb
{
    public NexusApiVerify(Configurator configurator, Client client)
    {
        _client = client;
        _renderer = configurator.Renderer;

    }

    public static VerbDefinition Definition => new("nexus-api-verify",
        "Verifies the logged in account via the Nexus API",
        Array.Empty<OptionDefinition>());

    private readonly Client _client;
    private readonly IRenderer _renderer;

    public async Task<int> Run(CancellationToken token)
    {
        var result = await _client.Validate(token);
        await _renderer.Render(new Table(new[] { "Name", "Premium", "Daily Remaining", "Hourly Remaining" },
            new[]
            {
                new object[]
                {
                    result.Data.Name, 
                    result.Data.IsPremium, 
                    result.Metadata.DailyRemaining,
                    result.Metadata.HourlyRemaining
                }
            }));

        return 0;
    }
}