using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Paths;

namespace NexusMods.Sdk.Tracking;

[PublicAPI]
public static class Events
{
    private static readonly EventString ModsDownloadCompletedName = "mods_download_completed";
    private static readonly EventString ModsDownloadFailedName = "mods_download_failed";
    private static readonly EventString ModsDownloadCancelledName = "mods_download_cancelled";

    private static readonly EventString ModsInstallationStartedName = "mods_installation_started";
    private static readonly EventString ModsInstallationCompletedName = "mods_installation_completed";
    private static readonly EventString ModsInstallationFailedName = "mods_installation_failed";
    private static readonly EventString ModsInstallationCancelledName = "mods_installation_cancelled";

    private static readonly EventString CollectionsDownloadCompletedName = "collections_download_completed";
    private static readonly EventString CollectionsDownloadFailedName = "collections_download_failed";

    private static readonly EventString AppLaunchedName = "app_launched";

    private static readonly EventString FileId = "file_id";
    private static readonly EventString ModId = "mod_id";
    private static readonly EventString GameId = "game_id";
    private static readonly EventString ModUid = "mod_uid";
    private static readonly EventString FileUid = "file_uid";
    private static readonly EventString FileSize = "file_size";
    private static readonly EventString Duration = "duration_ms";
    private static readonly EventString CollectionId = "collection_id";
    private static readonly EventString RevisionId = "revision_id";
    private static readonly EventString ModCount = "mod_count";

    public static void ModsDownloadCompleted(
        uint fileId,
        uint modId,
        uint gameId,
        ulong modUid,
        ulong fileUid,
        Size fileSize,
        long duration,
        Optional<ulong> collectionId = default,
        Optional<ulong> revisionId = default,
        IEventTracker? tracker = null)
    {
        (tracker ?? Tracker.EventTracker)?.Track(ModsDownloadCompletedName,
            (FileId, fileId),
            (ModId, modId),
            (GameId, gameId),
            (ModUid, modUid),
            (FileUid, fileUid),
            (Duration, duration),
            (FileSize, fileSize.Value),
            (CollectionId, collectionId.OrNull()),
            (RevisionId, revisionId.OrNull())
        );
    }

    public static void ModsDownloadFailed(
        uint fileId,
        uint modId,
        uint gameId,
        ulong modUid,
        ulong fileUid,
        Optional<ulong> collectionId = default,
        Optional<ulong> revisionId = default,
        IEventTracker? tracker = null)
    {
        (tracker ?? Tracker.EventTracker)?.Track(ModsDownloadFailedName,
            (FileId, fileId),
            (ModId, modId),
            (GameId, gameId),
            (ModUid, modUid),
            (FileUid, fileUid),
            (CollectionId, collectionId.OrNull()),
            (RevisionId, revisionId.OrNull())
        );
    }

    public static void ModsDownloadCancelled(
        uint fileId,
        uint modId,
        uint gameId,
        ulong modUid,
        ulong fileUid,
        Optional<ulong> collectionId = default,
        Optional<ulong> revisionId = default,
        IEventTracker? tracker = null)
    {
        (tracker ?? Tracker.EventTracker)?.Track(ModsDownloadCancelledName,
            (FileId, fileId),
            (ModId, modId),
            (GameId, gameId),
            (ModUid, modUid),
            (FileUid, fileUid),
            (CollectionId, collectionId.OrNull()),
            (RevisionId, revisionId.OrNull())
        );
    }

    public static void ModsInstallationStarted(
        uint fileId,
        uint modId,
        uint gameId,
        ulong modUid,
        ulong fileUid,
        IEventTracker? tracker = null)
    {
        (tracker ?? Tracker.EventTracker)?.Track(ModsInstallationStartedName,
            (FileId, fileId),
            (ModId, modId),
            (GameId, gameId),
            (ModUid, modUid),
            (FileUid, fileUid)
        );
    }

    public static void ModsInstallationCompleted(
        uint fileId,
        uint modId,
        uint gameId,
        ulong modUid,
        ulong fileUid,
        long duration,
        IEventTracker? tracker = null)
    {
        (tracker ?? Tracker.EventTracker)?.Track(ModsInstallationCompletedName,
            (FileId, fileId),
            (ModId, modId),
            (GameId, gameId),
            (ModUid, modUid),
            (FileUid, fileUid),
            (Duration, duration)
        );
    }

    public static void ModsInstallationFailed(
        uint fileId,
        uint modId,
        uint gameId,
        ulong modUid,
        ulong fileUid,
        IEventTracker? tracker = null)
    {
        (tracker ?? Tracker.EventTracker)?.Track(ModsInstallationFailedName,
            (FileId, fileId),
            (ModId, modId),
            (GameId, gameId),
            (ModUid, modUid),
            (FileUid, fileUid)
        );
    }

    public static void ModsInstallationCancelled(
        uint fileId,
        uint modId,
        uint gameId,
        ulong modUid,
        ulong fileUid,
        IEventTracker? tracker = null)
    {
        (tracker ?? Tracker.EventTracker)?.Track(ModsInstallationCancelledName,
            (FileId, fileId),
            (ModId, modId),
            (GameId, gameId),
            (ModUid, modUid),
            (FileUid, fileUid)
        );
    }

    public static void CollectionsDownloadCompleted(
        ulong collectionId,
        ulong revisionId,
        uint gameId,
        int modCount,
        long duration,
        IEventTracker? tracker = null)
    {
        (tracker ?? Tracker.EventTracker)?.Track(CollectionsDownloadCompletedName,
            (CollectionId, collectionId),
            (RevisionId, revisionId),
            (GameId, gameId),
            (ModCount, modCount),
            (Duration, duration)
        );
    }

    public static void CollectionsDownloadFailed(
        ulong collectionId,
        ulong revisionId,
        uint gameId,
        IEventTracker? tracker = null)
    {
        (tracker ?? Tracker.EventTracker)?.Track(CollectionsDownloadFailedName,
            (CollectionId, collectionId),
            (RevisionId, revisionId),
            (GameId, gameId)
        );
    }

    public static void AppLaunched(IEventTracker? tracker = null)
    {
        (tracker ?? Tracker.EventTracker)?.Track(AppLaunchedName);
    }
}
