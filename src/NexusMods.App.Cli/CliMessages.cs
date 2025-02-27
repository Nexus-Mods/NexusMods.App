using NexusMods.Abstractions.EventBus;
using NexusMods.Abstractions.NexusModsLibrary.Models;

namespace NexusMods.CLI;

public static class CliMessages
{
    public record AddedCollection(CollectionRevisionMetadata.ReadOnly Revision) : IEventBusMessage;

    public record AddedDownload() : IEventBusMessage;
}
