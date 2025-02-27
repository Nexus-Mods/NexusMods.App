using JetBrains.Annotations;
using Microsoft.AspNetCore.WebUtilities;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.NexusWebApi.Types.V2;

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

    /// <summary>
    /// Creates a Uri that sends the user to the website to download a file.
    /// This file will be installed via the NXM handler.
    /// </summary>
    /// <param name="fileId">Unique ID for a mod file associated with a game, <see cref="FileId"/>.</param>
    /// <param name="gameId">Unique identifier for an individual game hosted on Nexus.</param>
    /// <param name="withNxm">True if to use nxm handler, else false.</param>
    public static Uri CreateModFileDownloadUri(FileId fileId, GameId gameId, bool withNxm = true)
    {
        return CreateUri($"https://www.nexusmods.com/Core/Libs/Common/Widgets/DownloadPopUp?id={fileId}&game_id={gameId}&nmm={Convert.ToInt32(withNxm)}");
    }

    public static Uri CreateCollectionsUri(GameDomain gameDomain, CollectionSlug collectionSlug) => CreateUri($"https://next.nexusmods.com/{gameDomain}/collections/{collectionSlug}", campaign: "collections");

    public static Uri CreateCollectionRevisionUri(GameDomain gameDomain, CollectionSlug collectionSlug, RevisionNumber revisionNumber) => CreateUri($"https://next.nexusmods.com/{gameDomain}/collections/{collectionSlug}/revisions/{revisionNumber}", campaign: "collections");
   
    public static Uri CreateCollectionRevisionBugsUri(GameDomain gameDomain, CollectionSlug collectionSlug, RevisionNumber revisionNumber) => CreateUri($"https://next.nexusmods.com/{gameDomain}/collections/{collectionSlug}/revisions/{revisionNumber}/bugs", campaign: "collections");
    
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

