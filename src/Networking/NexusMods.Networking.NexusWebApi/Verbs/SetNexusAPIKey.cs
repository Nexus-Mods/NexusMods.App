using NexusMods.CLI;

namespace NexusMods.Networking.NexusWebApi.Verbs;

public class SetNexusAPIKey : AVerb<string>
{
    private readonly ApiKeyMessageFactory _factory;

    public SetNexusAPIKey(ApiKeyMessageFactory apiKeyMessageFactory)
    {
        _factory = apiKeyMessageFactory;
    }

    public static readonly VerbDefinition Definition = new("set-nexus-api-key",
        "Sets the key used in Nexus API calls",
        new[]
        {
            new OptionDefinition<string>("k", "key", "Key used in Nexus API call")
        });


    protected override async Task<int> Run(string key, CancellationToken token)
    {
        await _factory.SetApiKey(key);
        return 0;
    }
}