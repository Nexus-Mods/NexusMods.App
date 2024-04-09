using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Diff.ApplyDiff;

public partial class ApplyDiffView : ReactiveUserControl<IApplyDiffViewModel>
{
    public ApplyDiffView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
            {
                this.OneWayBind(ViewModel, vm => vm.BodyViewModel, v => v.TreeViewHost.ViewModel)
                    .DisposeWith(d);
            }
        );
    }
}

