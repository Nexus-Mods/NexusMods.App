using System.Reflection;

namespace NexusMods.App.BuildInfo;

/// <summary>
/// Constants supplied during runtime.
/// </summary>
public static class ApplicationConstants
{
    static ApplicationConstants()
    {
        try
        {
            // This attribute is set by SourceLink (https://github.com/dotnet/sourcelink)
            var attribute = Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyInformationalVersionAttribute));
            if (attribute is AssemblyInformationalVersionAttribute assemblyInformationalVersionAttribute)
            {
                var informationalVersion = assemblyInformationalVersionAttribute.InformationalVersion;
                var sha = GetSha(informationalVersion);
                CommitHash = sha;
            }
        }
        catch (Exception)
        {
            // ignored
        }

        var debugVersion = new Version(0, 0, 1);
        if (CompileConstants.IsDebug)
        {
            Version = debugVersion;
        }
        else
        {
            try
            {
                Version = Assembly.GetExecutingAssembly().GetName().Version ?? debugVersion;
            }
            catch (Exception)
            {
                Version = debugVersion;
            }
        }
    }

    private static string? GetSha(string input)
    {
        var span = input.AsSpan();
        var plusIndex = span.IndexOf('+');
        return plusIndex == -1 ? null : span[(plusIndex + 1)..].ToString();
    }

    /// <summary>
    /// Gets the current Version.
    /// </summary>
    public static Version Version { get; }

    /// <summary>
    /// Gets the hash of the current commit.
    /// </summary>
    public static string? CommitHash { get; }
}

