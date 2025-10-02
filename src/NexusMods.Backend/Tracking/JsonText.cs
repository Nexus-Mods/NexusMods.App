using System.Text.Json;
using NexusMods.Sdk;

namespace NexusMods.Backend.Tracking;

internal static class JsonText
{
    public static readonly JsonEncodedText AppNameValue = JsonEncodedText.Encode("Nexus Mods App");
    public static readonly JsonEncodedText AppVersionValue = JsonEncodedText.Encode(ApplicationConstants.Version.ToSafeString(maxFieldCount: 3));
    public static readonly JsonEncodedText PlatformTypeValue = JsonEncodedText.Encode("app");

    // NOTE(erri120): These tokens are for untrusted clients, they aren't considered secrets.
    // Please don't send automated e-mails about exposed secrets.
    private static readonly JsonEncodedText ProductionToken = JsonEncodedText.Encode("3f01fd6fe87f03fe7a053413929d76b7");
    private static readonly JsonEncodedText DevelopmentToken = JsonEncodedText.Encode("404c9346f400b7e5e3c65f2b1a1f3325");

    public static JsonEncodedText ProjectToken => ApplicationConstants.IsDebug ? DevelopmentToken : ProductionToken;

    public static readonly JsonEncodedText Event = JsonEncodedText.Encode("event");
    public static readonly JsonEncodedText Properties = JsonEncodedText.Encode("properties");
    public static readonly JsonEncodedText Token = JsonEncodedText.Encode("token");
    public static readonly JsonEncodedText Time = JsonEncodedText.Encode("time");
    public static readonly JsonEncodedText DistinctId = JsonEncodedText.Encode("distinct_id");
    public static readonly JsonEncodedText DeviceId = JsonEncodedText.Encode("$device_id");
    public static readonly JsonEncodedText UserId = JsonEncodedText.Encode("$user_id");
    public static readonly JsonEncodedText InsertId = JsonEncodedText.Encode("$insert_id");
    public static readonly JsonEncodedText OS = JsonEncodedText.Encode("$os");
    public static readonly JsonEncodedText PlatformType = JsonEncodedText.Encode("platform_type");
    public static readonly JsonEncodedText AppName = JsonEncodedText.Encode("app_name");
    public static readonly JsonEncodedText AppVersion = JsonEncodedText.Encode("app_version");

    // https://github.com/mixpanel/mixpanel-js/blob/8940b6a20ab4415f82c2c543583d946613158bf5/src/utils.js#L1554-L1576
    public static readonly JsonEncodedText Linux = JsonEncodedText.Encode("Linux");
    public static readonly JsonEncodedText Windows = JsonEncodedText.Encode("Windows");
    public static readonly JsonEncodedText OSX = JsonEncodedText.Encode("Mac OS X");

    public static readonly JsonEncodedText UserType = JsonEncodedText.Encode("user_type");
    public static readonly JsonEncodedText Anonymous = JsonEncodedText.Encode("anonymous");
    public static readonly JsonEncodedText Registered = JsonEncodedText.Encode("registered");
    public static readonly JsonEncodedText Premium = JsonEncodedText.Encode("premium");
    public static readonly JsonEncodedText Supporter = JsonEncodedText.Encode("supporter");
}
