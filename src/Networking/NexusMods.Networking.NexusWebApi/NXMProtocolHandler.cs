using Microsoft.Extensions.Logging;
using NexusMods.CLI.Types;
using NexusMods.DataModel.Interprocess;
using NexusMods.Networking.NexusWebApi.Types;

namespace NexusMods.Networking.NexusWebApi;

/// <summary>
/// a handler for nxm:// urls
/// </summary>
public class NXMProtocolHandler : IProtocolHandler
{
    /// <inheritdoc/>
    public string Protocol { get; } = "nxm";

    private ILogger<NXMProtocolHandler> _logger;
    private IMessageProducer<NXMUrlMessage> _messages;

    /// <summary>
    /// constructor
    /// </summary>
    public NXMProtocolHandler(ILogger<NXMProtocolHandler> logger, IMessageProducer<NXMUrlMessage> messages)
    {
        _logger = logger;
        _messages = messages;
    }

    /// <inheritdoc/>
    public async Task Handle(string url, CancellationToken cancel)
    {
        await _messages.Write(new NXMUrlMessage { Value = NXMUrl.Parse(url) }, cancel);
    }
}

