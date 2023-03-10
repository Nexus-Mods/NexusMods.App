namespace NexusMods.Paths;

public record FileVersionInfo(Version ProductVersion)
{
    public static Version ParseVersionString(string? input)
    {
        return input is null ? Version.Parse("1.0.0.0") : Version.Parse(input);
    }
}
