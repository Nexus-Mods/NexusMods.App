using NexusMods.Paths;

namespace NexusMods.DataModel.Games;

public struct GameLocationDescriptor
{
    private List<GameFolderType> _nestedLocations = new();

    public GameLocationDescriptor(GameFolderType id, AbsolutePath resolvedPath)
    {
        Id = id;
        ResolvedPath = resolvedPath;
    }

    public required GameFolderType Id { get; init; }

    public required AbsolutePath ResolvedPath { get; init; }

    public bool IsTopLevel { get; set; } = true;

    private GameFolderType? _topLevelParent = null;

    public GameFolderType? TopLevelParent => _topLevelParent;

    public IReadOnlyCollection<GameFolderType> NestedLocations => _nestedLocations;

    public void AddNestedLocation(GameLocationDescriptor nestedLocation)
    {
        nestedLocation.IsTopLevel = false;

        _nestedLocations.Add(nestedLocation.Id);
    }

    public void SetTopLevelParent(GameFolderType topLevelParent)
    {
        _topLevelParent = topLevelParent;
    }
}
