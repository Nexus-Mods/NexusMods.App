namespace NexusMods.Abstractions.Telemetry;

internal static class InstrumentConstants
{
    private const string Prefix = "app_";

    public const string NameActiveUsers        = Prefix + "active_users";
    public const string NameGlobalDownloadSize = Prefix + "global_download_size";
    public const string NameManagedGamesCount  = Prefix + "managed_games_count";
    public const string NameModsPerGame        = Prefix + "mods_per_game";
    public const string NameUsersPerLanguage   = Prefix + "users_per_language";
    public const string NameUsersPerMembership = Prefix + "users_per_membership";
    public const string NameUsersPerOS         = Prefix + "users_per_os";

    public const string TagGame       = "game";
    public const string TagLanguage   = "language";
    public const string TagMembership = "membership";
    public const string TagOS         = "os";
    public const string TagVersion    = "version";
}
