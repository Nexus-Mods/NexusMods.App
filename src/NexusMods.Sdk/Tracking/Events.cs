using System.Diagnostics;
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

    private static readonly EventString CollectionsInstallationStartedName = "collections_installation_started";
    private static readonly EventString CollectionsInstallationCompletedName = "collections_installation_completed";
    private static readonly EventString CollectionsInstallationFailedName = "collections_installation_failed";
    private static readonly EventString CollectionsInstallationCancelledName = "collections_installation_cancelled";

    private static readonly EventString AppLaunchedName = "app_launched";
    private static readonly EventString AppUninstalledName = "app_uninstalled";

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
        DurationValue duration,
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
            (Duration, duration.Milliseconds),
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
        DurationValue duration,
        IEventTracker? tracker = null)
    {
        (tracker ?? Tracker.EventTracker)?.Track(ModsInstallationCompletedName,
            (FileId, fileId),
            (ModId, modId),
            (GameId, gameId),
            (ModUid, modUid),
            (FileUid, fileUid),
            (Duration, duration.Milliseconds)
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
        DurationValue duration,
        IEventTracker? tracker = null)
    {
        (tracker ?? Tracker.EventTracker)?.Track(CollectionsDownloadCompletedName,
            (CollectionId, collectionId),
            (RevisionId, revisionId),
            (GameId, gameId),
            (ModCount, modCount),
            (Duration, duration.Milliseconds)
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

    public static void CollectionsInstallationStarted(
        ulong collectionId,
        ulong revisionId,
        uint gameId,
        int modCount,
        IEventTracker? tracker = null)
    {
        (tracker ?? Tracker.EventTracker)?.Track(CollectionsInstallationStartedName,
            (CollectionId, collectionId),
            (RevisionId, revisionId),
            (GameId, gameId),
            (ModCount, modCount)
        );
    }

    public static void CollectionsInstallationCompleted(
        ulong collectionId,
        ulong revisionId,
        uint gameId,
        int modCount,
        DurationValue duration,
        IEventTracker? tracker = null)
    {
        (tracker ?? Tracker.EventTracker)?.Track(CollectionsInstallationCompletedName,
            (CollectionId, collectionId),
            (RevisionId, revisionId),
            (GameId, gameId),
            (ModCount, modCount),
            (Duration, duration.Milliseconds)
        );
    }

    public static void AppLaunched(IEventTracker? tracker = null)
    {
        (tracker ?? Tracker.EventTracker)?.Track(AppLaunchedName);
    }

    public static void AppUninstalled(IEventTracker? tracker = null)
    {
        (tracker ?? Tracker.EventTracker)?.Track(AppUninstalledName);
    }

    public readonly record struct DurationValue(long Milliseconds)
    {
        public static implicit operator DurationValue(long value) => new(value);
        public static implicit operator DurationValue(TimeSpan value) => new((long)value.TotalMilliseconds);
        public static implicit operator DurationValue(Stopwatch value)
        {
            value.Stop();
            return new DurationValue(value.ElapsedMilliseconds);
        }
    }
}
