using NexusMods.Abstractions.Messaging;
using NexusMods.Abstractions.NexusWebApi.DTOs;
using NexusMods.Abstractions.NexusWebApi.Types;

namespace NexusMods.CLI.Types.IpcHandlers;

/// <summary>
/// a handler for nxm:// urls
/// </summary>
// ReSharper disable once InconsistentNaming
public class NxmIpcProtocolHandler : IIpcProtocolHandler
{
    /// <inheritdoc/>
    public string Protocol => "nxm";

    private readonly IMessageProducer<NXMUrlMessage> _messages;

    /// <summary>
    /// constructor
    /// </summary>
    public NxmIpcProtocolHandler(IMessageProducer<NXMUrlMessage> messages)
    {
        _messages = messages;
    }

    /// <inheritdoc/>
    public async Task Handle(string url, CancellationToken cancel)
    {
        await _messages.Write(new NXMUrlMessage { Value = NXMUrl.Parse(new Uri(url)) }, cancel);
    }
}

