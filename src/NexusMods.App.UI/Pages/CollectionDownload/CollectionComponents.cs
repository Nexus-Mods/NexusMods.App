using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.UI;
using NexusMods.Abstractions.UI.Extensions;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Pages.LibraryPage;
using NexusMods.MnemonicDB.Abstractions.Models;
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
            if (CompareImpl<CollectionComponents.InstallAction>(InstallComponentKey, a, b).TryGet(out var value)) return value;
            if (CompareImpl<CollectionComponents.ManualDownloadAction>(ManualDownloadComponentKey, a, b).TryGet(out value)) return value;
            if (CompareImpl<CollectionComponents.ExternalDownloadAction>(ExternalDownloadComponentKey, a, b).TryGet(out value)) return value;
            if (CompareImpl<CollectionComponents.NexusModsDownloadAction>(NexusModsDownloadComponentKey, a, b).TryGet(out value)) return value;
            return 0;

            static Optional<int> CompareImpl<TComponent>(ComponentKey key, CompositeItemModel<TKey> a, CompositeItemModel<TKey> b)
                where TComponent : class, IItemModelComponent<TComponent>, IComparable<TComponent>
            {
                var x = a.GetOptional<TComponent>(key);
                var y = b.GetOptional<TComponent>(key);

                return (x.HasValue, y.HasValue) switch
                {
                    (true, true) => x.Value.CompareTo(y.Value),
                    (true, false) => -1,
                    (false, true) => 1,
                    _ => Optional<int>.None,
                };
            }
        }

        public const string ColumnTemplateResourceKey = nameof(CollectionColumns) + "_" + nameof(Actions);
        public static readonly ComponentKey NexusModsDownloadComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(CollectionColumns) + "_" + "NexusModsDownload");
        public static readonly ComponentKey ExternalDownloadComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(CollectionColumns) + "_" + "ExternalDownload");
        public static readonly ComponentKey ManualDownloadComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(CollectionColumns) + "_" + "ManualDownload");
        public static readonly ComponentKey InstallComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(CollectionColumns) + "_" + "Install");

        public static string GetColumnHeader() => "Actions";
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }
}

public static class CollectionComponents
{
    public abstract class ADownloadAction<TSelf, TEntity> : ReactiveR3Object, IItemModelComponent<TSelf>, IComparable<TSelf>
        where TSelf : ADownloadAction<TSelf, TEntity>, IItemModelComponent<TSelf>, IComparable<TSelf>
        where TEntity : struct, IReadOnlyModel<TEntity>
    {
        public int CompareTo(TSelf? other)
        {
            if (other is null) return 1;
            return JobStatusComparer.Instance.Compare(DownloadStatus.Value, other.DownloadStatus.Value);
        }

        private readonly IDisposable _activationDisposable;
        protected ADownloadAction(
            TEntity downloadEntity,
            Observable<JobStatus> downloadJobStatusObservable,
            Observable<bool> isDownloadedObservable)
        {
            _activationDisposable = this.WhenActivated((downloadEntity, downloadJobStatusObservable, isDownloadedObservable), static (self, state, disposables) =>
            {
                var (_, downloadJobStatusObservable, isDownloadedObservable) = state;

                downloadJobStatusObservable.CombineLatest(isDownloadedObservable, static (a, b) => (a, b)).ObserveOnUIThreadDispatcher().Subscribe(self, static (tuple, self) =>
                {
                    var (downloadStatus, isDownloaded) = tuple;
                    self._canDownload.OnNext(!isDownloaded && downloadStatus < JobStatus.Running);
                    self._downloadStatus.Value = downloadStatus;
                    self._buttonText.Value = self.GetButtonText(isDownloading: downloadStatus == JobStatus.Running, isDownloaded);
                }).AddTo(disposables);
            });

            CommandDownload = _canDownload.ToReactiveCommand<Unit, TEntity>(_ => downloadEntity);

            IsDownloading = _downloadStatus
                .Select(status => status == JobStatus.Running)
                .ToReadOnlyBindableReactiveProperty(initialValue: false);
        }

        public ReactiveCommand<Unit, TEntity> CommandDownload { get; }

        private readonly BehaviorSubject<bool> _canDownload = new(initialValue: false);
        private readonly BindableReactiveProperty<JobStatus> _downloadStatus = new(value: JobStatus.None);
        public IReadOnlyBindableReactiveProperty<JobStatus> DownloadStatus => _downloadStatus;

        public IReadOnlyBindableReactiveProperty<bool> IsDownloading { get; }

        private readonly BindableReactiveProperty<string> _buttonText = new(value: "");
        public IReadOnlyBindableReactiveProperty<string> ButtonText => _buttonText;

        protected virtual string GetButtonText(bool isDownloading, bool isDownloaded)
        {
            if (isDownloaded) return "Downloaded";
            return isDownloading ? "Downloading" : "Download";
        }

        private bool _isDisposed;
        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                if (disposing)
                {
                    Disposable.Dispose(_activationDisposable,IsDownloading, CommandDownload, _canDownload, _buttonText, _downloadStatus);
                }
            }

            base.Dispose(disposing);
        }
    }

    public sealed class NexusModsDownloadAction : ADownloadAction<NexusModsDownloadAction, CollectionDownloadNexusMods.ReadOnly>
    {
        public NexusModsDownloadAction(
            CollectionDownloadNexusMods.ReadOnly downloadEntity,
            Observable<JobStatus> downloadJobStatusObservable,
            Observable<bool> isDownloadedObservable)
            : base(downloadEntity, downloadJobStatusObservable, isDownloadedObservable) { }
    }

    public sealed class ExternalDownloadAction : ADownloadAction<ExternalDownloadAction, CollectionDownloadExternal.ReadOnly>
    {
        public ExternalDownloadAction(
            CollectionDownloadExternal.ReadOnly downloadEntity,
            Observable<JobStatus> downloadJobStatusObservable,
            Observable<bool> isDownloadedObservable)
            : base(downloadEntity, downloadJobStatusObservable, isDownloadedObservable) { }

        protected override string GetButtonText(bool isDownloading, bool isDownloaded)
        {
            if (isDownloading || isDownloaded) return base.GetButtonText(isDownloading, isDownloaded);
            return "Third-party download";
        }
    }

    public sealed class ManualDownloadAction : ReactiveR3Object, IItemModelComponent<ManualDownloadAction>, IComparable<ManualDownloadAction>
    {
        public int CompareTo(ManualDownloadAction? other) => other is null ? 1 : 0;

        private readonly IDisposable _activationDisposable;
        public ManualDownloadAction(CollectionDownloadExternal.ReadOnly downloadEntity, Observable<bool> isDownloadedObservable)
        {
            CommandOpenModal = isDownloadedObservable.Select(static isDownloaded => !isDownloaded).ObserveOnUIThreadDispatcher().ToReactiveCommand<Unit, CollectionDownloadExternal.ReadOnly>(_ => downloadEntity);

            _activationDisposable = this.WhenActivated((downloadEntity, isDownloadedObservable), static (self, state, disposables) =>
            {
                var (_, isDownloadedObservable) = state;

                isDownloadedObservable.ObserveOnUIThreadDispatcher().Subscribe(self, static (isDownloaded, self) =>
                {
                    self._buttonText.Value = GetButtonText(isDownloaded);
                }).AddTo(disposables);
            });
        }

        private readonly BindableReactiveProperty<string> _buttonText = new(value: "");
        public IReadOnlyBindableReactiveProperty<string> ButtonText => _buttonText;

        public ReactiveCommand<Unit, CollectionDownloadExternal.ReadOnly> CommandOpenModal { get; }

        private static string GetButtonText(bool isDownloaded) => isDownloaded ? "Downloaded" : "Manual download";

        private bool _isDisposed;

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                if (disposing)
                {
                    Disposable.Dispose(_activationDisposable, CommandOpenModal, ButtonText);
                }
            }

            base.Dispose(disposing);
        }
    }

    public sealed class InstallAction : ReactiveR3Object, IItemModelComponent<InstallAction>, IComparable<InstallAction>
    {
        public int CompareTo(InstallAction? other)
        {
            if (other is null) return 1;
            return IsInstalled.Value.CompareTo(other.IsInstalled.Value);
        }

        private readonly IDisposable _activationDisposable;
        public InstallAction(
            CollectionDownloadEntity.ReadOnly downloadEntity,
            Observable<bool> isInstalledObservable)
        {
            CommandInstall = _canInstall
                .ToReactiveCommand<Unit, CollectionDownloadEntity.ReadOnly>(_ => downloadEntity);

            _activationDisposable = this.WhenActivated((downloadEntity, isInstalledObservable), static (self, state, disposables) =>
            {
                var (_, isInstalledObservable) = state;

                isInstalledObservable.Subscribe(self, static (isInstalled, self) =>
                {
                    self._canInstall.OnNext(!isInstalled);
                    self._isInstalled.Value = isInstalled;
                    self._buttonText.Value = LibraryComponents.InstallAction.GetButtonText(isInstalled);
                }).AddTo(disposables);
            });
        }

        private readonly BehaviorSubject<bool> _canInstall = new(initialValue: false);
        public ReactiveCommand<Unit, CollectionDownloadEntity.ReadOnly> CommandInstall { get; }

        private readonly BindableReactiveProperty<string> _buttonText = new(value: LibraryComponents.InstallAction.GetButtonText(isInstalled: false));
        public IReadOnlyBindableReactiveProperty<string> ButtonText => _buttonText;

        private readonly BindableReactiveProperty<bool> _isInstalled = new(value: false);
        public IReadOnlyBindableReactiveProperty<bool> IsInstalled => _isInstalled;

        private bool _isDisposed;
        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                _isDisposed = true;

                if (disposing)
                {
                    Disposable.Dispose(_activationDisposable, CommandInstall, _canInstall, _buttonText, _isInstalled);
                }
            }

            base.Dispose(disposing);
        }
    }
}
