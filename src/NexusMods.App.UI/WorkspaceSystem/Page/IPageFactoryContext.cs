namespace NexusMods.App.UI.WorkspaceSystem;

/// <summary>
/// Interface for contexts used by the <see cref="IPageFactory"/>.
/// </summary>
public interface IPageFactoryContext
{
    /// <summary>
    /// Gets whether the context is ephemeral in nature and should not be
    /// persisted.
    /// </summary>
    bool IsEphemeral => false;

    /// <summary>
    /// Alternative <see cref="PageData"/> to persist when <see cref="IsEphemeral"/> is <c>true</c>.
    /// </summary>
    PageData? GetSerializablePageData() => null;

    /// <summary>
    /// Gets the tracking name.
    /// </summary>
    string TrackingName => GetType().Name;
}
