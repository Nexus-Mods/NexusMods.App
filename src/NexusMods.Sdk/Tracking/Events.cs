using JetBrains.Annotations;

namespace NexusMods.Sdk.Tracking;

[PublicAPI]
public static class Events
{
    private static readonly EventPropertyDefinition FileId = EventPropertyDefinition.Create<uint>("file_id");
    private static readonly EventPropertyDefinition ModId = EventPropertyDefinition.Create<uint>("mod_id");
    private static readonly EventPropertyDefinition GameId = EventPropertyDefinition.Create<uint>("game_id");
    private static readonly EventPropertyDefinition ModUid = EventPropertyDefinition.Create<uint>("mod_uid");
    private static readonly EventPropertyDefinition FileUid = EventPropertyDefinition.Create<uint>("file_uid");
    private static readonly EventPropertyDefinition FileSize = EventPropertyDefinition.Create<uint>("file_size");
    private static readonly EventPropertyDefinition Duration = EventPropertyDefinition.Create<uint>("duration_ms");
    private static readonly EventPropertyDefinition CollectionId = EventPropertyDefinition.Create<ulong>("collection_id");
    private static readonly EventPropertyDefinition RevisionId = EventPropertyDefinition.Create<ulong>("revision_id");
    private static readonly EventPropertyDefinition ModCount = EventPropertyDefinition.Create<int>("mod_count");

    public static readonly EventDefinition ModsDownloadCompleted = new("mods_download_completed")
    {
        FileId,
        ModId,
        GameId,
        ModUid,
        FileUid,
        FileSize,
        Duration,
        CollectionId.AsOptional(),
        RevisionId.AsOptional(),
    };

    public static readonly EventDefinition ModsDownloadFailed = new("mods_download_failed")
    {
        FileId,
        ModId,
        GameId,
        ModUid,
        FileUid,
        CollectionId.AsOptional(),
        RevisionId.AsOptional(),
    };

    public static readonly EventDefinition ModsDownloadCancelled = new("mods_download_cancelled")
    {
        FileId,
        ModId,
        GameId,
        ModUid,
        FileUid,
        CollectionId.AsOptional(),
        RevisionId.AsOptional(),
    };

    public static readonly EventDefinition ModsInstallationStarted = new("mods_installation_started")
    {
        FileId,
        ModId,
        GameId,
        ModUid,
        FileUid,
    };

    public static readonly EventDefinition ModsInstallationCompleted = new("mods_installation_completed")
    {
        FileId,
        ModId,
        GameId,
        ModUid,
        FileUid,
        Duration,
    };

    public static readonly EventDefinition ModsInstallationFailed = new("mods_installation_failed")
    {
        FileId,
        ModId,
        GameId,
        ModUid,
        FileUid,
    };

    public static readonly EventDefinition ModsInstallationCancelled = new("mods_installation_cancelled")
    {
        FileId,
        ModId,
        GameId,
        ModUid,
        FileUid,
    };

    public static readonly EventDefinition CollectionsDownloadCompleted = new("collections_download_completed")
    {
        CollectionId,
        RevisionId,
        ModCount,
        Duration,
        GameId,
    };

    public static readonly EventDefinition CollectionsDownloadFailed = new("collections_download_failed")
    {
        CollectionId,
        RevisionId,
        GameId,
    };

    public static readonly EventDefinition AppLaunched = new("app_launched");
}
