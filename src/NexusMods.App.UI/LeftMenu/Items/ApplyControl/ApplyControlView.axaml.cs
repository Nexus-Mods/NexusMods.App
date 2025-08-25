using System.Reactive.Disposables;
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

                this.BindCommand(ViewModel, vm => vm.ShowApplyDiffCommand, v => v.PreviewChangesButton)
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel, vm => vm.LaunchButtonViewModel, v => v.LaunchButtonView.ViewModel)
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel, vm => vm.IsLaunchButtonEnabled, v => v.LaunchButtonView.IsEnabled)
                    .DisposeWith(disposables);
                
                this.OneWayBind(ViewModel, vm => vm.IsProcessing, v => v.ProcessingChangesStackPanel.IsVisible)
                    .DisposeWith(disposables);
                
                this.OneWayBind(ViewModel, vm => vm.IsApplying, v => v.ProgressBarControl.IsVisible)
                    .DisposeWith(disposables);

                this.WhenAnyObservable(view => view.ViewModel!.ApplyCommand.CanExecute)
                    .OnUI()
                    .Subscribe(canApply =>
                        {
                            ApplyButton.IsVisible = canApply;
                            PreviewChangesButton.IsVisible = canApply;
                        }
                    )
                    .DisposeWith(disposables);

                this.OneWayBind(ViewModel, vm => vm.ApplyButtonText, v => v.ApplyButtonTextBlock.Text)
                    .DisposeWith(disposables);
                
                this.OneWayBind(ViewModel, vm => vm.ProcessingText, v => v.ProgressBarControl.ProgressTextFormat)
                    .DisposeWith(disposables);
            }
        );
    }
}
