namespace NexusMods.Sdk;

/// <summary>
/// Environment variables
/// </summary>
internal static class EnvironmentVariableNames
{
    /// <summary>
    /// Prefix for all environment variables.
    /// </summary>
    private const string Prefix = "NMA_";

    /// <summary>
    /// Debug mode.
    /// </summary>
    public const string IsDebug = Prefix + "DEBUG";

    /// <summary>
    /// Version.
    /// </summary>
    public const string Version = Prefix + "VERSION";

    /// <summary>
    /// User agent name.
    /// </summary>
    public const string UserAgentName = Prefix + "USER_AGENT";

    /// <summary>
    /// Installation method.
    /// </summary>
    public const string InstallationMethod = Prefix + "INSTALLATION_METHOD";
}
