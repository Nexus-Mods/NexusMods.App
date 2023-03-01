using Microsoft.Extensions.Logging;
using NexusMods.CLI.Types;

namespace NexusMods.CLI.Verbs;

public class ProtocolInvocation : AVerb<string>
{
    public static VerbDefinition Definition => new("protocol-invoke",
        "Handle a URL with custom protocol",
        new OptionDefinition[]
        {
            new OptionDefinition<string>("u", "url", "The URL to handle")
        });

    private IEnumerable<IProtocolHandler> _handlers;

    public ProtocolInvocation(ILogger logger, List<IProtocolHandler> handlers)
    {
        _handlers = handlers;
        logger.LogInformation("Number of handlers: {Handlers}", handlers.Count);
    }

    public async Task<int> Run(string url, CancellationToken cancel)
    {
        var uri = new Uri(url);
        var handler = _handlers.FirstOrDefault(iter => iter.Protocol == uri.Scheme);
        if (handler == null)
        {
            throw new Exception($"Unsupported protocol \"{uri.Scheme}\"");
        }

        await handler.Handle(url, cancel);

        return 0;
    }
}
