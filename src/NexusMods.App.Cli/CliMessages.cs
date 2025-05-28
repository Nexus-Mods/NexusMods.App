using NexusMods.Sdk.EventBus;
using NexusMods.Abstractions.NexusModsLibrary.Models;

namespace NexusMods.CLI;

/// <summary>
/// Messages passed from the CLI to the main application.
/// </summary>
public static class CliMessages
{
    /// <summary>
    /// A new collection was added to the app.
    /// </summary>
    public record AddedCollection(CollectionRevisionMetadata.ReadOnly Revision) : IEventBusMessage;

    /// <summary>
    /// A new download was added to the app.
    /// </summary>
    public record AddedDownload() : IEventBusMessage;

    /// <summary>
    /// A protocol registration test was initialized.
    /// </summary>
    public record TestProtocolRegistration(Guid Id) : IEventBusMessage;
}
