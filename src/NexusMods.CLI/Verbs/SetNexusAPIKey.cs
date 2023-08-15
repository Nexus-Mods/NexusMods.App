using NexusMods.Abstractions.CLI;
using NexusMods.Networking.NexusWebApi.NMA;

// Temporary until moved to CLI project.
#pragma warning disable CS1591
namespace NexusMods.CLI.Verbs;

// ReSharper disable once InconsistentNaming
public class SetNexusAPIKey : AVerb<string>
{
    private readonly ApiKeyMessageFactory _factory;

    public SetNexusAPIKey(ApiKeyMessageFactory apiKeyMessageFactory)
    {
        _factory = apiKeyMessageFactory;
    }

    public static VerbDefinition Definition => new("set-nexus-api-key",
        "Sets the key used in Nexus API calls",
        new OptionDefinition[]
        {
            new OptionDefinition<string>("k", "key", "Key used in Nexus API call")
        });


    public async Task<int> Run(string key, CancellationToken token)
    {
        await _factory.SetApiKey(key);
        return 0;
    }
}
