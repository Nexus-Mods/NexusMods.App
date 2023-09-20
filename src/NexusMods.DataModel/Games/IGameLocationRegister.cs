using NexusMods.Paths;

namespace NexusMods.DataModel.Games;

public interface IGameLocationRegister
{
    public bool IsTopLevel(GameFolderType id);

    public GameFolderType GetTopLevelParent(GameFolderType id);

    public AbsolutePath GetResolvedPath(GameFolderType id);

    public AbsolutePath GetResolvedPath(GamePath path);

    public IReadOnlyCollection<GameFolderType> GetNestedLocations (GameFolderType Id);

    public IReadOnlyDictionary<GameFolderType, GameLocationDescriptor> LocationDescriptors { get; }

    public GamePath ToGamePath(AbsolutePath absolutePath);

    public IReadOnlyCollection<KeyValuePair<GameFolderType, AbsolutePath>> GetTopLevelLocations();

}
