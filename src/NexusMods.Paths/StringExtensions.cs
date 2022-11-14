namespace NexusMods.Paths;

public static class StringExtensions
{
    public static RelativePath ToRelativePath(this string s)
    {
        return (RelativePath)s;
    }

    public static AbsolutePath ToAbsolutePath(this string s)
    {
        return (AbsolutePath)s;
    }
}