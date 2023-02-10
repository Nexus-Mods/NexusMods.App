namespace NexusMods.Paths;

public readonly struct Extension
{
    private static readonly Extension None = new("");
    public int Length => _extension.Length;

    #region ObjectEquality

    private bool Equals(Extension other)
    {
        return string.Equals(_extension, other._extension, StringComparison.InvariantCultureIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        return obj is Extension other && Equals(other);
    }

    public override string ToString()
    {
        return _extension;
    }

    public override int GetHashCode()
    {
        return _extension.GetHashCode(StringComparison.InvariantCultureIgnoreCase);
    }

    #endregion

    private readonly string _extension;

    public Extension(string extension)
    {
        _extension = extension;
        Validate();
    }

    private void Validate()
    {
        if (!_extension.StartsWith(".") && _extension != "")
            throw new PathException($"Extensions must start with '.' got {_extension}");
    }

    public static explicit operator string(Extension path)
    {
        return path._extension;
    }

    public static explicit operator Extension(string path)
    {
        return new Extension(path);
    }

    public static bool operator ==(Extension a, Extension b)
    {
        return string.Equals(a._extension, b._extension, StringComparison.InvariantCultureIgnoreCase);
    }

    public static bool operator !=(Extension a, Extension b)
    {
        return !(a == b);
    }

    public static Extension FromPath(string path)
    {
        var lastIndex = path.LastIndexOf(".", StringComparison.Ordinal);
        return lastIndex == -1 ? None : new Extension(path[lastIndex..]);
    }
}