using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Localization;
using NexusMods.App.UI.Resources;
using NexusMods.DataModel.RateLimiting;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.Spine.Buttons.Download;

public partial class DownloadButtonView : ReactiveUserControl<IDownloadButtonViewModel>
{

    public DownloadButtonView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            new LocalizedStringUpdater(() =>
            {
                ViewModel!.IdleText = Language.DownloadStatus_Idle;
                ViewModel!.ProgressText = Language.DownloadStatus_Progress;
            }).DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.Click)
                .BindToUi(this, view => view.ParentButton.Command)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.Progress, view => view.ViewModel!.IdleText,
                    view => view.ViewModel!.ProgressText)
                .Select<(Percent?, string, string), bool>(tuple => tuple.Item1 != null)
                .BindToClasses(ParentButton, Language.DownloadStatus_Idle, Language.DownloadStatus_Progress)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.IsActive)
                .BindToActive(ParentButton)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.Progress)
                .WhereNotNull()
                .Select(p => p!.Value.Value * 360)
                .BindToUi(this, vm => vm.ProgressArc.SweepAngle)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.Number)
                .Select(n => n.ToString("###0.00"))
                .BindToUi(this, view => view.NumberTextBlock.Text)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.Units)
                .Select(n => n.ToUpperInvariant())
                .BindToUi(this, view => view.UnitsTextBlock.Text)
                .DisposeWith(d);
        });
    }
}

