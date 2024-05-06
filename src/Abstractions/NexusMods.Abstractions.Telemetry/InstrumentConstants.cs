namespace NexusMods.Abstractions.Telemetry;

internal static class InstrumentConstants
{
    private const string Prefix = "app_";

    public const string NameActiveUsers        = Prefix + "active_users";
    public const string NameUsersPerLanguage   = Prefix + "users_per_language";
    public const string NameUsersPerMembership = Prefix + "users_per_membership";
    public const string NameUsersPerOS         = Prefix + "users_per_os";

    public const string TagLanguage   = "language";
    public const string TagMembership = "membership";
    public const string TagOS         = "os";
}
