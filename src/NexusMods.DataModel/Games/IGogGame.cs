namespace NexusMods.DataModel.Games;

/// <summary>
/// Marker interface for the Gog store locator
/// </summary>
public interface IGogGame : IGame
{
    /// <summary>
    /// Returns a list of gog game ids
    /// </summary>
    IEnumerable<long> GogIds { get; }

}
