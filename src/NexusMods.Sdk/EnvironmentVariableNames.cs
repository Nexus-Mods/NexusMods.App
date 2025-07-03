namespace NexusMods.Sdk;

/// <summary>
/// Environment variables
/// </summary>
public static class EnvironmentVariableNames
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

    /// <summary>
    /// Base domain for Nexus Mods.
    /// </summary>
    public const string NexusModsBaseDomain = Prefix + "NEXUS_MODS_BASE_DOMAIN";

    /// <summary>
    /// Subdomain for the API.
    /// </summary>
    public const string NexusModsApiSubdomain = Prefix + "NEXUS_MODS_API_SUBDOMAIN";

    /// <summary>
    /// Subdomain for the users.
    /// </summary>
    public const string NexusModsUsersSubdomain = Prefix + "NEXUS_MODS_USERS_SUBDOMAIN";
}
