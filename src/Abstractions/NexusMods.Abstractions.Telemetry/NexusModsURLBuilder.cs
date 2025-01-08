using JetBrains.Annotations;
using Microsoft.AspNetCore.WebUtilities;
using NexusMods.Abstractions.NexusWebApi.Types;

namespace NexusMods.Abstractions.Telemetry;

// NOTE(erri120): This class consolidates all URL tracking parameter related
// functionality. Anything that exposes a URL to the user that points to
// Nexus Mods should use this class.
// Everything in here is hardcoded and parameter values shouldn't be changed.
// New methods can be added after talking with the team about this.

[PublicAPI]
public static class NexusModsUrlBuilder
{
    private const string ParameterValueSource = "nexusmodsapp";

    // From https://matomo.org/faq/reports/what-is-campaign-tracking-and-why-it-is-important/
    private const string ParameterNameCampaign = "mtm_campaign";
    private const string ParameterNameKeyword  = "mtm_keyword";
    private const string ParameterNameMedium   = "mtm_medium";
    private const string ParameterNameSource   = "mtm_source";
    private const string ParameterNameContent  = "mtm_content";
    private const string ParameterNameGroup    = "mtm_group";

    /// <summary>
    /// Creates a new URI by adding tracking parameters to the given <paramref name="baseUrl"/>.
    /// </summary>
    public static Uri CreateUri(string baseUrl, string? campaign = null, string? medium = null)
    {
        var parameters = new Dictionary<string, string?>
        {
            { ParameterNameSource, ParameterValueSource },
            { ParameterNameCampaign, campaign },
            { ParameterNameMedium, medium },
        };

        var updated = QueryHelpers.AddQueryString(baseUrl, parameters);
        return new Uri(updated);
    }

    /// <summary>
    /// Creates a new URI by adding tracking parameters to the given <paramref name="baseUrl"/>.
    /// </summary>
    /// <remarks>
    /// Use this method if you have a generic URL to Nexus Mods.
    /// </remarks>
    public static Uri CreateGenericUri(string baseUrl) => CreateUri(baseUrl);

    public static Uri CreateCollectionsUri(GameDomain gameDomain, CollectionSlug collectionSlug) => CreateUri($"https://next.nexusmods.com/{gameDomain}/collections/{collectionSlug}", campaign: "collections");

    public static Uri LearAboutPremiumUri => CreateUri("https://next.nexusmods.com/premium");

    public static Uri UpgradeToPremiumUri => CreateUri("https://users.nexusmods.com/account/billing/premium");

    /// <summary>
    /// Creates a new URI pointing to a mod on Nexus Mods. This should only be used
    /// by diagnostics.
    /// </summary>
    public static Uri CreateDiagnosticUri(string game, string modId)
    {
        return CreateUri($"https://nexusmods.com/{game}/mods/{modId}", campaign: "diagnostics");
    }
}

