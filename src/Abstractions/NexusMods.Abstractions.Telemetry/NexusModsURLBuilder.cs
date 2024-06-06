using JetBrains.Annotations;
using Microsoft.AspNetCore.WebUtilities;

namespace NexusMods.Abstractions.Telemetry;

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

    private static Uri CreateUri(string baseUrl, string? campaign = null, string? medium = null)
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

    public static Uri CreateGenericUri(string baseUrl) => CreateUri(baseUrl);

    public static Uri CreateDiagnosticUri(string game, string modId)
    {
        return CreateUri($"https://nexusmods.com/{game}/mods/{modId}", campaign: "diagnostics");
    }
}

