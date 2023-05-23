using NexusMods.DataModel.Interprocess;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Networking.NexusWebApi.NMA.Messages;
using NexusMods.Networking.NexusWebApi.Types;

namespace NexusMods.CLI.Types.IpcHandlers;

/// <summary>
/// a handler for nxm:// urls
/// </summary>
// ReSharper disable once InconsistentNaming
public class NxmIpcProtocolHandler : IIpcProtocolHandler
{
    /// <inheritdoc/>
    public string Protocol { get; } = "nxm";

    private IMessageProducer<NXMUrlMessage> _messages;

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
        await _messages.Write(new NXMUrlMessage { Value = NXMUrl.Parse(url) }, cancel);
    }
}

