namespace NexusMods.Paths;

/// <summary>
///     The base folder for the GamePath, more values can easily be added here as needed
/// </summary>
public enum GameFolderType : byte
{
    Game = 0,
    Saves,
    Profiles
}

public struct GamePath : IPath
{
    public GamePath(GameFolderType folder, RelativePath path)
    {
        Folder = folder;
        Path = path;
    }
    
    public GamePath(GameFolderType folder, string path) : this(folder, path.ToRelativePath())
    {
    }
    public bool Equals(GamePath other)
    {
        return Folder == other.Folder && Path == other.Path;
    }

    public static bool operator ==(GamePath a, GamePath b)
    {
        return a.Folder == b.Folder && a.Path == b.Path;
    }

    public static bool operator !=(GamePath a, GamePath b)
    {
        return a.Folder != b.Folder || a.Path != b.Path;
    }

    public override bool Equals(object? obj)
    {
        return obj is GamePath other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Path.GetHashCode() ^ (int)Folder;
    }

    public RelativePath Path { get; }
    public GameFolderType Folder { get; }



    public override string ToString()
    {
        return "{" + Folder + "}\\" + Path;
    }

    public Extension Extension => Path.Extension;
    public RelativePath FileName => Path.FileName;
}