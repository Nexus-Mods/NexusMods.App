using NexusMods.CLI.Types;
using NexusMods.DataModel.Interprocess;
using NexusMods.Networking.NexusWebApi.Types;

namespace NexusMods.Networking.NexusWebApi;

/// <summary>
/// a handler for nxm:// urls
/// </summary>
// ReSharper disable once InconsistentNaming
public class NXMProtocolHandler : IProtocolHandler
{
    /// <inheritdoc/>
    public string Protocol { get; } = "nxm";

    private IMessageProducer<NXMUrlMessage> _messages;

    /// <summary>
    /// constructor
    /// </summary>
    public NXMProtocolHandler(IMessageProducer<NXMUrlMessage> messages)
    {
        _messages = messages;
    }

    /// <inheritdoc/>
    public async Task Handle(string url, CancellationToken cancel)
    {
        await _messages.Write(new NXMUrlMessage { Value = NXMUrl.Parse(url) }, cancel);
    }
}

