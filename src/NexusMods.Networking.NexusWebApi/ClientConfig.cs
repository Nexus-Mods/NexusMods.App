using NexusMods.Sdk;

namespace NexusMods.Networking.NexusWebApi;

internal static class ClientConfig
{
    private const string DefaultBaseDomain = "nexusmods.com";
    private const string DefaultApiSubdomain = "api";
    private const string DefaultUsersSubdomain = "users";

    static ClientConfig()
    {
        if (!EnvironmentVariables.TryGetString(EnvironmentVariableNames.NexusModsBaseDomain, out var baseDomain))
            baseDomain = DefaultBaseDomain;
        if (!EnvironmentVariables.TryGetString(EnvironmentVariableNames.NexusModsApiSubdomain, out var apiSubdomain))
            apiSubdomain = DefaultApiSubdomain;
        if (!EnvironmentVariables.TryGetString(EnvironmentVariableNames.NexusModsUsersSubdomain, out var usersSubdomain))
            usersSubdomain = DefaultUsersSubdomain;

        BaseUrl = new Uri($"https://{baseDomain}");
        ApiUrl = new Uri($"https://{apiSubdomain}.{baseDomain}");
        UsersUrl = new Uri($"https://{usersSubdomain}.{baseDomain}");
        LegacyApiEndpoint = new Uri($"https://{apiSubdomain}.{baseDomain}v1");
        GraphQlEndpoint = new Uri($"https://{apiSubdomain}.{baseDomain}v2/graphql");
        OAuthUrl = new Uri($"{UsersUrl}oauth");
    }

    public static readonly Uri BaseUrl;
    public static readonly Uri ApiUrl;
    public static readonly Uri UsersUrl;
    public static readonly Uri LegacyApiEndpoint;
    public static readonly Uri GraphQlEndpoint;
    public static readonly Uri OAuthUrl;
}
