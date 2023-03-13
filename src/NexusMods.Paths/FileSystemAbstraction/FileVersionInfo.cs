namespace NexusMods.Paths;

/// <summary>
/// Represents version information for a file on disk.
/// </summary>
/// <param name="ProductVersion">Gets the version of the product this file is distributed with.</param>
public record struct FileVersionInfo(Version ProductVersion)
{
    /// <summary>
    /// Parses the input as a <see cref="Version"/>.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static Version ParseVersionString(string? input)
    {
        return input is null ? Version.Parse("1.0.0.0") : Version.Parse(input);
    }
}
