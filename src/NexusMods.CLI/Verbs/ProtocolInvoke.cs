using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.CLI;
using NexusMods.CLI.Types;

namespace NexusMods.CLI.Verbs;

/// <summary>
/// Handle a URL with custom protocol
/// </summary>
public class ProtocolInvoke : AVerb<string>
{
    /// <inheritdoc />
    public static VerbDefinition Definition => new("protocol-invoke",
        "Handle a URL with custom protocol",
        new OptionDefinition[]
        {
            new OptionDefinition<string>("u", "url", "The URL to handle")
        });

    private IIpcProtocolHandler[] _handlers;
    private readonly ILogger<ProtocolInvoke> _logger;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="handlers"></param>
    public ProtocolInvoke(ILogger<ProtocolInvoke> logger, IEnumerable<IIpcProtocolHandler> handlers)
    {
        _handlers = handlers.ToArray();
        _logger = logger;
        logger.LogInformation("Number of handlers: {Handlers}", _handlers.Length);
    }

    /// <inheritdoc />
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
