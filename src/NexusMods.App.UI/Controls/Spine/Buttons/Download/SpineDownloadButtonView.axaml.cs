using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.Extensions;
using ReactiveUI;
using static NexusMods.App.UI.Helpers.StyleConstants.SpineDownloadButton;

namespace NexusMods.App.UI.Controls.Spine.Buttons.Download;

public partial class SpineDownloadButtonView : ReactiveUserControl<ISpineDownloadButtonViewModel>
{
    public SpineDownloadButtonView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(view => view.ViewModel!.Click)
                .BindToUi(this, view => view.ParentButton.Command)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.Progress)
                .Select(p => p.HasValue)
                .BindToClasses(ParentButton, Progress, Idle)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.IsActive)
                .BindToActive(ParentButton)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.Progress)
                .Where(p => p.HasValue)
                .Select(p => p.Value.Value * 360)
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

