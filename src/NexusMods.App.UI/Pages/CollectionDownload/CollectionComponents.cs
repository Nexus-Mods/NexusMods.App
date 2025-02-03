using JetBrains.Annotations;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls;
using R3;

namespace NexusMods.App.UI.Pages.CollectionDownload;
using CollectionDownloadEntity = NexusMods.Abstractions.NexusModsLibrary.Models.CollectionDownload;

public static class CollectionColumns
{
    [UsedImplicitly]
    public sealed class Actions : ICompositeColumnDefinition<Actions>
    {
        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            throw new NotImplementedException();
        }

        public const string ColumnTemplateResourceKey = nameof(CollectionColumns) + "_" + nameof(Actions);
        public static readonly ComponentKey NexusModsDownloadComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(CollectionColumns) + "_" + "NexusModsDownload");
        public static readonly ComponentKey ExternalDownloadComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(CollectionColumns) + "_" + "ExternalDownload");
        public static readonly ComponentKey ManualDownloadComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(CollectionColumns) + "_" + "ManualDownload");
        public static readonly ComponentKey InstallComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(CollectionColumns) + "_" + "Install");

        public static string GetColumnHeader() => "Action";
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }
}

public static class CollectionComponents
{
    public sealed class NexusModsDownloadAction : ReactiveR3Object, IItemModelComponent<NexusModsDownloadAction>, IComparable<NexusModsDownloadAction>
    {
        public int CompareTo(NexusModsDownloadAction? other)
        {
            if (other is null) return 1;
            return JobStatusComparer.Instance.Compare(DownloadStatus.Value, other.DownloadStatus.Value);
        }

        private readonly CollectionDownloadNexusMods.ReadOnly _downloadEntity;

        public NexusModsDownloadAction(
            CollectionDownloadNexusMods.ReadOnly downloadEntity,
            Observable<JobStatus> downloadJobStatusObservable)
        {
            _downloadEntity = downloadEntity;

            DownloadStatus = downloadJobStatusObservable.ToReadOnlyBindableReactiveProperty(initialValue: JobStatus.None);
            CommandDownload = downloadJobStatusObservable
                .Select(status => status < JobStatus.Running)
                .ToReactiveCommand<Unit, CollectionDownloadNexusMods.ReadOnly>(_ => _downloadEntity);
        }

        public ReactiveCommand<Unit, CollectionDownloadNexusMods.ReadOnly> CommandDownload { get; }

        public IReadOnlyBindableReactiveProperty<JobStatus> DownloadStatus { get; }

        private bool _isDisposed;
        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    Disposable.Dispose(CommandDownload, DownloadStatus);
                }

                _isDisposed = true;
            }

            base.Dispose(disposing);
        }
    }

    public sealed class ExternalDownloadAction : ReactiveR3Object, IItemModelComponent<ExternalDownloadAction>, IComparable<ExternalDownloadAction>
    {
        public int CompareTo(ExternalDownloadAction? other)
        {
            // TODO:
            return 0;
        }
    }

    public sealed class ManualDownloadAction : ReactiveR3Object, IItemModelComponent<ManualDownloadAction>, IComparable<ManualDownloadAction>
    {
        public int CompareTo(ManualDownloadAction? other)
        {
            // TODO:
            return 0;
        }
    }

    public sealed class InstallAction : ReactiveR3Object, IItemModelComponent<InstallAction>, IComparable<InstallAction>
    {
        public int CompareTo(InstallAction? other)
        {
            // TODO:
            return 0;
        }
    }
}
