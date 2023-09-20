using System.Diagnostics;
using NexusMods.Paths;

namespace NexusMods.DataModel.Games;

public class GameLocationsRegister
{
    private Dictionary<GameFolderType, GameLocationDescriptor> _locations = new();

    public AbsolutePath this[GameFolderType id] => _locations[id].ResolvedPath;

    public GameLocationsRegister(IReadOnlyDictionary<GameFolderType, AbsolutePath> newLocations)
    {
        foreach (var (newId, newPath) in newLocations)
        {
            var newLocation = new GameLocationDescriptor { Id = newId, ResolvedPath = newPath };

            foreach (var existingLocation in _locations.Values)
            {
                if (existingLocation.ResolvedPath.GetFullPathLength() < newPath.GetFullPathLength())
                {
                    if (!newPath.InFolder(existingLocation.ResolvedPath)) continue;

                    existingLocation.AddNestedLocation(newLocation);
                    newLocation.SetTopLevelParent(ComputeTopLevelParent(newLocation, existingLocation));
                }
                else
                {
                    if (!existingLocation.ResolvedPath.InFolder(newPath)) continue;

                    newLocation.AddNestedLocation(existingLocation);
                    existingLocation.SetTopLevelParent(ComputeTopLevelParent(existingLocation, newLocation));
                }
            }

            var isDuplicate = _locations.TryAdd(newId, newLocation);

            Debug.Assert(isDuplicate == false,
                $"Duplicate location found for {newId} at {_locations[newId].ResolvedPath}: {newPath}");
        }
    }

    private GameFolderType ComputeTopLevelParent(GameLocationDescriptor child, GameLocationDescriptor newParent)
    {
        if (child.TopLevelParent == null)
        {
            return newParent.TopLevelParent ?? newParent.Id;
        }

        var previousToLevelParent = _locations[child.TopLevelParent.Value];
        var newParentTopParent = _locations[newParent.TopLevelParent ?? newParent.Id];

        // If the new parent path is shorter it means that it is the new top level parent
        return newParentTopParent.ResolvedPath.GetFullPathLength() <
               previousToLevelParent.ResolvedPath.GetFullPathLength()
            ? newParentTopParent.Id
            : child.TopLevelParent.Value;
    }

    public IReadOnlyDictionary<GameFolderType, GameLocationDescriptor> LocationDescriptors => _locations;

    public bool IsTopLevel(GameFolderType id)
    {
        return _locations[id].IsTopLevel;
    }

    public GameFolderType GetTopLevelParent(GameFolderType id)
    {
        return _locations[id].TopLevelParent ?? id;
    }

    public AbsolutePath GetResolvedPath(GameFolderType id)
    {
        return _locations[id].ResolvedPath;
    }

    public AbsolutePath GetResolvedPath(GamePath path)
    {
        return _locations[path.Type].ResolvedPath.Combine(path.Path);
    }

    public IReadOnlyCollection<GameFolderType> GetNestedLocations(GameFolderType Id)
    {
        return _locations[Id].NestedLocations.ToArray();
    }

    public GamePath ToGamePath(AbsolutePath absolutePath)
    {
        return _locations.Values.Where(location => absolutePath.InFolder(location.ResolvedPath))
            .Select(desc => new GamePath(desc.Id, absolutePath.RelativeTo(desc.ResolvedPath)))
            .MinBy(gamePath => gamePath.Path.Depth);
    }

    public IReadOnlyCollection<KeyValuePair<GameFolderType, AbsolutePath>> GetTopLevelLocations()
    {
        return LocationDescriptors
            .Where(x => x.Value.IsTopLevel)
            .Select(x => new KeyValuePair<GameFolderType, AbsolutePath>(x.Key, x.Value.ResolvedPath))
            .ToArray();
    }
}
