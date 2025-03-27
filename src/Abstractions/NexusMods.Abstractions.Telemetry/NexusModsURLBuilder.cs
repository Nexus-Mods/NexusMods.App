using DynamicData.Kernel;
using JetBrains.Annotations;
using Microsoft.AspNetCore.WebUtilities;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.NexusWebApi.Types.V2;

namespace NexusMods.Abstractions.Telemetry;

/// NOTE(erri120): This class consolidates all URL tracking parameter related
/// functionality. Anything that exposes a URL to the user that points to
/// Nexus Mods should use this class.
/// Everything in here is hardcoded and parameter values shouldn't be changed.
/// New methods can be added after talking with the team about this.
[PublicAPI]
public static class NexusModsUrlBuilder
{
    private const string BaseUrl = "https://www.nexusmods.com";
    private const string UsersBaseUrl = "https://users.nexusmods.com";
    private const string ParameterValueSource = "nexusmodsapp";

    // From https://matomo.org/faq/reports/what-is-campaign-tracking-and-why-it-is-important/
    private const string ParameterNameCampaign = "mtm_campaign";
    private const string ParameterNameKeyword  = "mtm_keyword";
    private const string ParameterNameMedium   = "mtm_medium";
    private const string ParameterNameSource   = "mtm_source";
    private const string ParameterNameContent  = "mtm_content";
    private const string ParameterNameGroup    = "mtm_group";

    /// <summary>
    /// Campaign value for everything related to updating mods in the app.
    /// </summary>
    public const string CampaignUpdates = "updates";

    /// <summary>
    /// Campaign value for everything related to using collections in the app.
    /// </summary>
    public const string CampaignCollections = "collections";

    /// <summary>
    /// Campaign value for everything related to Nexus Mods Premium.
    /// </summary>
    public const string CampaignPremium = "premium";

    /// <summary>
    /// Campaign values for everything related to diagnostics.
    /// </summary>
    public const string CampaignDiagnostics = "diagnostics";

    /// <summary>
    /// Creates a new URI by adding tracking parameters to the given <paramref name="baseUrl"/>.
    /// </summary>
    public static Uri CreateUri(string baseUrl, string? source = ParameterValueSource, string? campaign = null, string? medium = null)
    {
        var parameters = new Dictionary<string, string?>
        {
            { ParameterNameSource, source },
            { ParameterNameCampaign, campaign },
            { ParameterNameMedium, medium },
        };

        var updated = QueryHelpers.AddQueryString(baseUrl, parameters);
        return new Uri(updated);
    }

    /// <summary>
    /// Uri for the user settings page.
    /// </summary>
    /// <example>
    /// <c>https://users.nexusmods.com</c>
    /// </example>
    public static readonly Uri UserSettingsUri = CreateUri(UsersBaseUrl);

    /// <summary>
    /// Returns a URI for a user profile.
    /// </summary>
    public static Uri GetProfileUri(UserId userId, string? source = ParameterValueSource, string? campaign = null)
    {
        // https://www.nexusmods.com/users/6672467
        var url = $"{BaseUrl}/users/{userId}";
        return CreateUri(url, source: source, campaign: campaign);
    }

    /// <summary>
    /// Returns a URI for a game page.
    /// </summary>
    public static Uri GetGameUri(GameDomain gameDomain, string? source = ParameterValueSource, string? campaign = null)
    {
        // https://www.nexusmods.com/games/stardewvalley
        var url = $"{BaseUrl}/games/{gameDomain}";
        return CreateUri(url, source: source, campaign: campaign);
    }

    /// <summary>
    /// Returns a URI for a mod page.
    /// </summary>
    public static Uri GetModUri(GameDomain gameDomain, ModId modId, string? source = ParameterValueSource, string? campaign = null)
    {
        // https://www.nexusmods.com/stardewvalley/mods/2400
        var url = $"{BaseUrl}/{gameDomain}/mods/{modId}";
        return CreateUri(url, source: source, campaign: campaign);
    }

    /// <summary>
    /// Returns a URI for a file download page.
    /// </summary>
    /// <remarks>
    /// <paramref name="useNxmLink"/> changes how the download button on the page behaves. If set to
    /// <c>true</c>, the download button will open an NXM link, if set to <c>false</c> the download
    /// will happen in the browser.
    /// </remarks>
    public static Uri GetFileDownloadUri(GameDomain gameDomain, ModId modId, FileId fileId, bool useNxmLink, string? source = ParameterValueSource, string? campaign = null)
    {
        // https://www.nexusmods.com/stardewvalley/mods/2400?tab=files&file_id=128328&nmm=0
        // https://www.nexusmods.com/stardewvalley/mods/2400?tab=files&file_id=128328&nmm=1
        var url = $"{BaseUrl}/{gameDomain}/mods/{modId}?tab=files&file_id={fileId}&nmm={Convert.ToInt32(useNxmLink)}";
        return CreateUri(url, source: source, campaign: campaign);
    }

    /// <summary>
    /// Returns a URI for a collection page.
    /// </summary>
    public static Uri GetCollectionUri(GameDomain gameDomain, CollectionSlug collectionSlug, Optional<RevisionNumber> revisionNumber, string? source = ParameterValueSource, string? campaign = null)
    {
        // https://www.nexusmods.com/games/stardewvalley/collections/tckf0m
        // https://www.nexusmods.com/games/stardewvalley/collections/tckf0m/revisions/80
        var url = $"{BaseUrl}/games/{gameDomain}/collections/{collectionSlug}{(revisionNumber.HasValue ? $"/revisions/{revisionNumber.Value}" : string.Empty)}";
        return CreateUri(url, source: source, campaign: campaign);
    }

    /// <summary>
    /// Returns a URI for the bugs page of a collection.
    /// </summary>
    public static Uri GetCollectionBugsUri(GameDomain gameDomain, CollectionSlug collectionSlug, Optional<RevisionNumber> revisionNumber, string? source = ParameterValueSource, string? campaign = null)
    {
        // https://www.nexusmods.com/games/stardewvalley/collections/tckf0m/revisions/80/bugs
        var url = $"{BaseUrl}/games/{gameDomain}/collections/{collectionSlug}{(revisionNumber.HasValue ? $"/revisions/{revisionNumber.Value}" : string.Empty)}/bugs";
        return CreateUri(url, source: source, campaign: campaign);
    }

    /// <summary>
    /// Uri for the premium benefits page.
    /// </summary>
    /// <example>
    /// <c>https://www.nexusmods.com/premium</c>
    /// </example>
    public static readonly Uri LearAboutPremiumUri = CreateUri($"{BaseUrl}/premium", campaign: CampaignPremium);

    /// <summary>
    /// Uri for the upgrade to premium page.
    /// </summary>
    /// <example>
    /// <c></c>
    /// </example>
    public static readonly Uri UpgradeToPremiumUri = CreateUri($"{UsersBaseUrl}/account/billing/premium", campaign: CampaignPremium);
}
