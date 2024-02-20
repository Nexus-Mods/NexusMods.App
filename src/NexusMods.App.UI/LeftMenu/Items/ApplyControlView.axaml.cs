using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Items;

public partial class ApplyControlView : ReactiveUserControl<IApplyControlViewModel>
{
    public ApplyControlView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.BindCommand(ViewModel, vm => vm.ApplyCommand, v => v.ApplyButton)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.LaunchButtonViewModel, v => v.LaunchButtonView.ViewModel)
                .DisposeWith(disposables);

            this.WhenAnyValue(view => view.ViewModel!.CanApply)
                .OnUI()
                .BindToView(this, view => view.ApplyButton.IsVisible)
                .DisposeWith(disposables);

            this.WhenAnyValue(view => view.ViewModel!.IsApplying)
                .OnUI()
                .BindToView(this, view => view.InProgressBorder.IsVisible)
                .DisposeWith(disposables);

            this.WhenAnyValue(view => view.ViewModel!.IsApplying)
                .Select(isApplying => !isApplying)
                .OnUI()
                .BindToView(this, view => view.LaunchButtonView.IsVisible)
                .DisposeWith(disposables);

        });
    }
}
