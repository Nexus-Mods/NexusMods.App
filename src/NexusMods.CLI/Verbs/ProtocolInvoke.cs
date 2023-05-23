using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NexusMods.CLI.Types;

namespace NexusMods.CLI.Verbs;

public class ProtocolInvoke : AVerb<string>
{
    public static VerbDefinition Definition => new("protocol-invoke",
        "Handle a URL with custom protocol",
        new OptionDefinition[]
        {
            new OptionDefinition<string>("u", "url", "The URL to handle")
        });

    private IProtocolHandler[] _handlers;
    private readonly ILogger<ProtocolInvoke> _logger;

    public ProtocolInvoke(ILogger<ProtocolInvoke> logger, IEnumerable<IProtocolHandler> handlers)
    {
        _handlers = handlers.ToArray();
        _logger = logger;
        logger.LogInformation("Number of handlers: {Handlers}", _handlers.Length);
    }

    public async Task<int> Run(string url, CancellationToken cancel)
    {
        var uri = new Uri(url);
        var handler = _handlers.FirstOrDefault(iter => iter.Protocol == uri.Scheme);
        if (handler == null)
            throw new Exception($"Unsupported protocol \"{uri.Scheme}\"");

        _logger.LogDebug("Handling {Url} with {Handler}", url, handler.GetType().Name);
        await handler.Handle(url, cancel);

        return 0;
    }
}
