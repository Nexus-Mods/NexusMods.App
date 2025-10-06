using JetBrains.Annotations;

namespace NexusMods.Sdk.Tracking;

[PublicAPI]
public static class Events
{
    public static readonly EventDefinition ModsDownloadCompleted = new("mods_download_completed")
    {
        EventPropertyDefinition.Create<uint>("file_id"),
        EventPropertyDefinition.Create<uint>("mod_id"),
        EventPropertyDefinition.Create<uint>("game_id"),
        EventPropertyDefinition.Create<ulong>("mod_uid"),
        EventPropertyDefinition.Create<ulong>("file_uid"),
        EventPropertyDefinition.Create<ulong>("file_size"),
        EventPropertyDefinition.Create<long>("duration_ms"),
        EventPropertyDefinition.Create<ulong>("collection_id", isOptional: true),
        EventPropertyDefinition.Create<ulong>("revision_id", isOptional: true),
    };

    public static readonly EventDefinition AppLaunched = new("app_launched");
}
