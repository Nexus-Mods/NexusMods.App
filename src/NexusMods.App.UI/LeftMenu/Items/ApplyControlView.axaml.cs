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
            
            this.BindCommand(ViewModel, vm => vm.IngestCommand, v => v.IngestButton)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.LaunchButtonViewModel, v => v.LaunchButtonView.ViewModel)
                .DisposeWith(disposables);
            
            this.WhenAnyObservable(view => view.ViewModel!.ApplyCommand.CanExecute)
                .OnUI()
                .BindToView(this, view => view.ApplyButton.IsVisible)
                .DisposeWith(disposables);
            
            this.WhenAnyObservable(view => view.ViewModel!.ApplyCommand.IsExecuting)
                .OnUI()
                .BindToView(this, view => view.InProgressBorder.IsVisible)
                .DisposeWith(disposables);
            
            this.OneWayBind(ViewModel, vm => vm.ApplyButtonText, v => v.ApplyButtonTextBlock.Text)
                .DisposeWith(disposables);

            this.WhenAnyObservable(view => view.ViewModel!.ApplyCommand.IsExecuting)
                .Select(isApplying => !isApplying)
                .OnUI()
                .BindToView(this, view => view.LaunchButtonView.IsVisible)
                .DisposeWith(disposables);

        });
    }
}
