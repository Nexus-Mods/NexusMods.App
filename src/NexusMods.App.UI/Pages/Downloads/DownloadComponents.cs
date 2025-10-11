using Humanizer;
using Humanizer.Bytes;
using NexusMods.Abstractions.Downloads;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Controls.Filters;
using NexusMods.App.UI.Controls.TreeDataGrid.Filters;
using NexusMods.App.UI.Resources;
using NexusMods.Paths;
using NexusMods.Sdk.Jobs;
using NexusMods.UI.Sdk;
using R3;

namespace NexusMods.App.UI.Pages.Downloads;

/// <summary>
/// DownloadRef: Reference holder for DownloadInfo objects
/// - This component is static and never changes
/// </summary>
public sealed class DownloadRef(DownloadInfo download) : ReactiveR3Object, IItemModelComponent<DownloadRef>, IComparable<DownloadRef>
{
    public DownloadId DownloadId { get; } = download.Id;
    public DownloadInfo Download { get; } = download;

    public int CompareTo(DownloadRef? other)
    {
        if (other is null) return 1;
        return DownloadId.CompareTo(other.DownloadId);
    }

    public FilterResult MatchesFilter(Filter filter) => FilterResult.Indeterminate;
}

/// <summary>
    /// Components for Downloads data display.
    /// </summary>
public static class DownloadComponents
{
    /// <summary>
    /// GAME COLUMN COMPONENT
    /// - Shows game name and game icon
    /// - This is static, never changes.
    /// </summary>
    public sealed class GameComponent(string gameName) : ReactiveR3Object, IItemModelComponent<GameComponent>, IComparable<GameComponent>
    {
        public IReadOnlyBindableReactiveProperty<string> GameName { get; } = new BindableReactiveProperty<string>(gameName);

        public int CompareTo(GameComponent? other)
        {
            if (other is null) return 1;
            return string.Compare(GameName.Value, other.GameName.Value, StringComparison.OrdinalIgnoreCase);
        }

        public FilterResult MatchesFilter(Filter filter)
        {
            return filter switch
            {
                Filter.NameFilter nameFilter => GameName.Value.Contains(
                    nameFilter.SearchText, 
                    nameFilter.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase)
                    ? FilterResult.Pass : FilterResult.Fail,
                Filter.TextFilter textFilter => GameName.Value.Contains(
                    textFilter.SearchText, 
                    textFilter.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase)
                    ? FilterResult.Pass : FilterResult.Fail,
                _ => FilterResult.Indeterminate,
            };
        }

        private bool _isDisposed;
        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing) GameName.Dispose();
                _isDisposed = true;
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// SIZE COLUMN COMPONENT
    /// - Displays format: "15.6MB of 56MB" (downloaded of total)
    /// - No ETA calculations (removed per requirements)
    /// </summary>
    public sealed class SizeProgressComponent : ReactiveR3Object, IItemModelComponent<SizeProgressComponent>, IComparable<SizeProgressComponent>
    {
        public IReadOnlyBindableReactiveProperty<string> DisplayText { get; }
        public IReadOnlyBindableReactiveProperty<Size> DownloadedBytes { get; }
        public IReadOnlyBindableReactiveProperty<Size> TotalSize { get; }

        public SizeProgressComponent(
            Size initialDownloaded,
            Size initialTotal,
            Observable<Size> downloadedObservable,
            Observable<Size> totalObservable)
        {
            DownloadedBytes = downloadedObservable.ToBindableReactiveProperty(initialDownloaded);
            TotalSize = totalObservable.ToBindableReactiveProperty(initialTotal);
            DisplayText = downloadedObservable
                .CombineLatest(totalObservable, FormatSizeProgress)
                .ToBindableReactiveProperty(FormatSizeProgress(initialDownloaded, initialTotal));
        }

        public int CompareTo(SizeProgressComponent? other)
        {
            if (other is null) return 1;
            return TotalSize.Value.CompareTo(other.TotalSize.Value);
        }

        public FilterResult MatchesFilter(Filter filter)
        {
            return filter switch
            {
                Filter.SizeRangeFilter sizeFilter => 
                    (TotalSize.Value >= sizeFilter.MinSize && TotalSize.Value <= sizeFilter.MaxSize)
                    ? FilterResult.Pass : FilterResult.Fail,
                Filter.TextFilter textFilter => DisplayText.Value.Contains(
                    textFilter.SearchText, 
                    textFilter.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase)
                    ? FilterResult.Pass : FilterResult.Fail,
                _ => FilterResult.Indeterminate,
            };
        }

        private static string FormatSizeProgress(Size downloaded, Size total)
        {
            var downloadedSize = ByteSize.FromBytes(downloaded.Value);
            var totalSize = ByteSize.FromBytes(total.Value);
            
            var downloadedStr = downloadedSize.Gigabytes < 1 ? downloadedSize.Humanize("0") : downloadedSize.Humanize("0.0");
            var totalStr = totalSize.Gigabytes < 1 ? totalSize.Humanize("0") : totalSize.Humanize("0.0");
                
            return $"{downloadedStr}{Language.Downloads_SizeProgress_Of}{totalStr}";
        }

        private bool _isDisposed;
        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                    Disposable.Dispose(DisplayText, DownloadedBytes, TotalSize);

                _isDisposed = true;
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// SPEED COLUMN COMPONENT
    /// - Shows transfer rate: "5.2 MB/s" or "--" when inactive
    /// </summary>
    public sealed class SpeedComponent : ReactiveR3Object, IItemModelComponent<SpeedComponent>, IComparable<SpeedComponent>
    {
        public IReadOnlyBindableReactiveProperty<Size> TransferRate { get; }
        public IReadOnlyBindableReactiveProperty<string> DisplayText { get; }

        public SpeedComponent(
            Size initialTransferRate,
            Observable<Size> transferRateObservable)
        {
            TransferRate = transferRateObservable.ToBindableReactiveProperty(initialTransferRate);
            DisplayText = transferRateObservable
                .Select(FormatTransferRate)
                .ToBindableReactiveProperty(FormatTransferRate(initialTransferRate));
        }

        public int CompareTo(SpeedComponent? other)
        {
            if (other is null) return 1;
            return TransferRate.Value.CompareTo(other.TransferRate.Value);
        }

        public FilterResult MatchesFilter(Filter filter)
        {
            return filter switch
            {
                Filter.SizeRangeFilter sizeFilter => 
                    (TransferRate.Value >= sizeFilter.MinSize && TransferRate.Value <= sizeFilter.MaxSize)
                    ? FilterResult.Pass : FilterResult.Fail,
                Filter.TextFilter textFilter => DisplayText.Value.Contains(
                    textFilter.SearchText, 
                    textFilter.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase)
                    ? FilterResult.Pass : FilterResult.Fail,
                _ => FilterResult.Indeterminate,
            };
        }

        private static string FormatTransferRate(Size rate)
        {
            if (rate.Value <= 0) return Language.Downloads_Speed_Inactive;
            return new ByteRate(ByteSize.FromBytes(rate.Value), TimeSpan.FromSeconds(1)).Humanize();
        }

        private bool _isDisposed;
        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                    Disposable.Dispose(TransferRate, DisplayText);

                _isDisposed = true;
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// STATUS COLUMN COMPONENT
    /// - Contains embedded controls: progress bar, pause/resume button, cancel button, kebab menu
    /// - All download actions consolidated into this single column
    /// </summary>
    public sealed class StatusComponent : ReactiveR3Object, IItemModelComponent<StatusComponent>, IComparable<StatusComponent>
    {
        public IReadOnlyBindableReactiveProperty<double> Progress { get; }
        public IReadOnlyBindableReactiveProperty<JobStatus> Status { get; }
        public IReadOnlyBindableReactiveProperty<bool> IsPaused { get; }
        
        // Commands
        public ReactiveCommand<Unit> PauseCommand { get; } = new();
        public ReactiveCommand<Unit> ResumeCommand { get; } = new();
        public ReactiveCommand<Unit> CancelCommand { get; } = new();
        
        // Visibility based on JobStatus
        public IReadOnlyBindableReactiveProperty<bool> CanPause { get; }
        public IReadOnlyBindableReactiveProperty<bool> CanResume { get; }
        public IReadOnlyBindableReactiveProperty<bool> CanCancel { get; }
        public IReadOnlyBindableReactiveProperty<bool> IsCompleted { get; }

        public StatusComponent(
            Percent initialProgress,
            JobStatus initialStatus,
            Observable<Percent> progressObservable,
            Observable<JobStatus> statusObservable)
        {
            Progress = progressObservable.Select(p => p.Value).ToBindableReactiveProperty(initialProgress.Value);
            Status = statusObservable.ToBindableReactiveProperty(initialStatus);
            IsPaused = statusObservable.Select(status => status == JobStatus.Paused).ToBindableReactiveProperty(initialStatus == JobStatus.Paused);

            // Set up can-execute properties based on status  
            CanPause = statusObservable
                .Select(static status => status == JobStatus.Running)
                .ToBindableReactiveProperty(initialStatus == JobStatus.Running);

            CanResume = statusObservable
                .Select(static status => status == JobStatus.Paused)
                .ToBindableReactiveProperty(initialStatus == JobStatus.Paused);

            CanCancel = statusObservable
                .Select(static status => status is JobStatus.Created or JobStatus.Running or JobStatus.Paused)
                .ToBindableReactiveProperty(initialStatus is JobStatus.Created or JobStatus.Running or JobStatus.Paused);

            IsCompleted = statusObservable
                .Select(static status => status == JobStatus.Completed)
                .ToBindableReactiveProperty(initialStatus == JobStatus.Completed);
        }

        public int CompareTo(StatusComponent? other)
        {
            if (other is null) return 1;
            return Status.Value.CompareTo(other.Status.Value);
        }

        public FilterResult MatchesFilter(Filter filter)
        {
            return filter switch
            {
                Filter.TextFilter textFilter => Status.Value.ToString().Contains(
                    textFilter.SearchText, 
                    textFilter.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase)
                    ? FilterResult.Pass : FilterResult.Fail,
                _ => FilterResult.Indeterminate,
            };
        }

        private bool _isDisposed;
        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                    Disposable.Dispose(Progress, Status, PauseCommand, ResumeCommand, CancelCommand, CanPause, CanResume, CanCancel, IsCompleted);

                _isDisposed = true;
            }
            base.Dispose(disposing);
        }
    }
}
