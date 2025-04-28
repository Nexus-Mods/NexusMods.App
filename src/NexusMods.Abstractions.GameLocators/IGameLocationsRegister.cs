using NexusMods.Paths;

namespace NexusMods.Abstractions.GameLocators;

/// <summary>
///     Provides access to various locations tied to
/// </summary>
public interface IGameLocationsRegister
{
    /// <summary>
    ///     Obtain the resolved path for a <see cref="LocationId" />.
    /// </summary>
    /// <param name="id">The <see cref="LocationId" /> to lookup</param>
    AbsolutePath this[LocationId id] { get; }

    /// <summary>
    /// Dictionary of <see cref="LocationId"/> and <see cref="GameLocationDescriptor"/>s.
    /// </summary>
    public IReadOnlyDictionary<LocationId, GameLocationDescriptor> LocationDescriptors { get; }

    /// <summary>
    /// [Use for Test Only!]
    /// Resets the <see cref="IGameLocationsRegister"/> to the passed locations.
    /// </summary>
    public void Reset(IDictionary<LocationId, AbsolutePath> locations);

    /// <summary>
    ///     Returns the associated <see cref="AbsolutePath" /> for the current game installation.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    AbsolutePath GetResolvedPath(LocationId id);

    /// <summary>
    ///     Translates the <see cref="GamePath" /> to a resolved <see cref="AbsolutePath" /> for the current game installation.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    AbsolutePath GetResolvedPath(GamePath path);

    /// <summary>
    ///     Returns a collection of <see cref="LocationId" />s that are nested directories of the passed location.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    IReadOnlyCollection<LocationId> GetNestedLocations(LocationId id);

    /// <summary>
    /// Maps the <see cref="AbsolutePath"/> to a <see cref="GamePath"/> for the current game installation.
    /// </summary>
    /// <remarks>
    /// In case of multiple nested locations containing the path,
    /// this method will return a <see cref="GamePath"/> for the "closest" <see cref="LocationId"/> to the file.
    /// E.g. if "Data" location is nested to "Game", and the path is "Game/Data/foo.bar", the GamePath will be relative to "Data".
    /// </remarks>
    /// <param name="absolutePath"></param>
    /// <returns></returns>
    GamePath ToGamePath(AbsolutePath absolutePath);

    /// <summary>
    ///     Returns the collection of game locations that are not nested to any other,
    ///     in the form of a collection of <see cref="KeyValuePair" /> of <see cref="LocationId" />,
    ///     <see cref="AbsolutePath" /> />
    /// </summary>
    /// <remarks>
    ///     If there are two top level locations with the same path, the first one in the order they were declared will be
    ///     returned.
    /// </remarks>
    IReadOnlyCollection<KeyValuePair<LocationId, AbsolutePath>> GetTopLevelLocations();
}
