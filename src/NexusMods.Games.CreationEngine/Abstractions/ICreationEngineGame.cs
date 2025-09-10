namespace NexusMods.Games.CreationEngine.Abstractions;


/// <summary>
/// A common interface for all games that use the creation engine.
/// </summary>
public interface ICreationEngineGame
{
    /// <summary>
    /// Get the plugin utilities for this game.
    /// </summary>
    public IPluginUtilities PluginUtilities { get; }
}
