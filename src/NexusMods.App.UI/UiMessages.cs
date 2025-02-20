using NexusMods.Abstractions.NexusModsLibrary.Models;

namespace NexusMods.App.UI;

public static class UiMessages
{
    public record AddedCollection(CollectionRevisionMetadata.ReadOnly Revision) : IEventBusMessage;
}
