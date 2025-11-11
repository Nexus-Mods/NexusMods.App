using NexusMods.Sdk.EventBus;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Sdk.Models.Library;

namespace NexusMods.CLI;

/// <summary>
/// Messages passed from the CLI to the main application.
/// </summary>
public static class CliMessages
{
    /// <summary>
    /// A new collection is being added to the app.
    /// </summary>
    public record CollectionAddStarted() : IEventBusMessage;
    
    /// <summary>
    /// A new collection was added to the app.
    /// </summary>
    public record CollectionAddSucceeded(CollectionRevisionMetadata.ReadOnly Revision) : IEventBusMessage;
    
    /// <summary>
    /// A new collection failed to be added to the app.
    /// </summary>
    public record CollectionAddFailed(IFailureReason Reason) : IEventBusMessage;

    /// <summary>
    /// A new mod download was added to the app.
    /// </summary>
    public record ModDownloadStarted() : IEventBusMessage;
    
    /// <summary>
    /// A mod download was successfully completed.
    /// </summary>
    public record ModDownloadSucceeded(LibraryItem.ReadOnly LibraryItem) : IEventBusMessage;
    
    /// <summary>
    /// A mod download failed.
    /// </summary>
    public record ModDownloadFailed(IFailureReason Reason) : IEventBusMessage;
    
    /// <summary>
    /// A protocol registration test was initialized.
    /// </summary>
    public record TestProtocolRegistration(Guid Id) : IEventBusMessage;
}

public interface IFailureReason { }

public static class FailureReason
{
    public record Unknown(Exception Exception) : IFailureReason;

    public record NotLoggedIn : IFailureReason;
    
    public record AlreadyExists(string Name) : IFailureReason;
    
    public record GameNotManaged(string Game) : IFailureReason;
}
