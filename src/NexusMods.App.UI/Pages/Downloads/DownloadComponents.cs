using Humanizer;
using Humanizer.Bytes;
using NexusMods.Abstractions.Downloads;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.UI;
using NexusMods.Abstractions.UI.Extensions;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Controls.Filters;
using NexusMods.App.UI.Controls.TreeDataGrid.Filters;
using NexusMods.App.UI.Resources;
using NexusMods.Paths;
using R3;

namespace NexusMods.App.UI.Pages.Downloads;

/*
 * DOWNLOADS UI COMPONENTS OVERVIEW
 * ================================
 * 
 * This file contains the reactive components used to display download data in the Downloads TreeDataGrid.
 * The Downloads UI consists of 5 main columns with the following structure:
 * 
 * COLUMNS & COMPONENTS:
 * 1. Name+Icon Column:
 *    - Uses DownloadColumns.Name.NameComponentKey (from this file)
 *    - Uses DownloadColumns.Name.ImageComponentKey (from this file)
 *    - Icon should show mod/download icon when available.
 * 
 * 2. Game Column:  
 *    - Uses DownloadComponents.GameComponent (defined in this file)
 *    - Shows game name and game icon
 * 
 * 3. Size Column:
 *    - Uses DownloadComponents.SizeProgressComponent (defined in this file)
 *    - Displays format: "15.6MB of 56MB" (downloaded of total)
 *    - No ETA calculations (removed per requirements)
 * 
 * 4. Speed Column:
 *    - Uses DownloadComponents.SpeedComponent (defined in this file)
 *    - Shows transfer rate: "5.2 MB/s" or "--" when inactive
 * 
 * 5. Status Column:
 *    - Uses DownloadComponents.StatusComponent (defined in this file)
 *    - Contains embedded controls: progress bar, pause/resume button, cancel button, kebab menu
 *    - All download actions consolidated into this single column
 * 
 * ADDITIONAL COMPONENTS:
 * - DownloadRef: Reference holder for DownloadInfo objects (defined as top-level in this file)
 * - Column definitions are in DownloadColumns.cs
 * 
 * REACTIVE ARCHITECTURE:
 * - All components use R3 reactive properties for real-time updates
 * - Subscribe to DownloadInfo property changes via ReactiveUI.WhenAnyValue()
 * - Proper disposal management with WhenActivated patterns
 */

/// <summary>
/// Component that holds a reference to the download information.
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
    /// Component that displays game name for each download.
    /// </summary>
    /// <remarks>
    ///     This is static, never changes.
    /// </remarks>
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
    /// Component that displays download progress in "15.6 MB of 56 MB" format.
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
    /// Component that displays transfer rate as "5.2 MB/s" or "--" when inactive.
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
    /// Component with embedded progress bar, pause/resume, cancel, and kebab menu.
    /// </summary>
    public sealed class StatusComponent : ReactiveR3Object, IItemModelComponent<StatusComponent>, IComparable<StatusComponent>
    {
        public IReadOnlyBindableReactiveProperty<double> Progress { get; }
        public IReadOnlyBindableReactiveProperty<JobStatus> Status { get; }
        
        // Commands
        public ReactiveCommand<Unit> PauseCommand { get; } = new();
        public ReactiveCommand<Unit> ResumeCommand { get; } = new();
        public ReactiveCommand<Unit> CancelCommand { get; } = new();
        
        // Visibility based on JobStatus
        public IReadOnlyBindableReactiveProperty<bool> CanPause { get; }
        public IReadOnlyBindableReactiveProperty<bool> CanResume { get; }
        public IReadOnlyBindableReactiveProperty<bool> CanCancel { get; }

        public StatusComponent(
            Percent initialProgress,
            JobStatus initialStatus,
            Observable<Percent> progressObservable,
            Observable<JobStatus> statusObservable)
        {
            Progress = progressObservable.Select(p => p.Value).ToBindableReactiveProperty(initialProgress.Value);
            Status = statusObservable.ToBindableReactiveProperty(initialStatus);

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
                    Disposable.Dispose(Progress, Status, PauseCommand, ResumeCommand, CancelCommand, CanPause, CanResume, CanCancel);

                _isDisposed = true;
            }
            base.Dispose(disposing);
        }
    }
}
