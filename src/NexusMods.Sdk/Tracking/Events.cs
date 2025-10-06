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
        EventPropertyDefinition.Create<uint>("mod_uid"),
        EventPropertyDefinition.Create<uint>("file_uid"),
        EventPropertyDefinition.Create<ulong>("file_size"),
        EventPropertyDefinition.Create<long>("duration_ms"),
        EventPropertyDefinition.Create<uint>("collection_id", isOptional: true),
        EventPropertyDefinition.Create<uint>("revision_id", isOptional: true),
    };
}
