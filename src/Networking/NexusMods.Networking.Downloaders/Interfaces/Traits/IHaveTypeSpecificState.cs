namespace NexusMods.Networking.Downloaders.Interfaces.Traits;

/// <summary>
/// A trait for state specific to certain <see cref="IDownloadTask"/>s to expose their non-shared state.
/// </summary>
public interface IHaveTypeSpecificState<out TState>
{
    /// <summary>
    /// Gets the type specific state.
    /// </summary>
    public TState GetState();
}
