using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Items;

public partial class LaunchButtonView : ReactiveUserControl<ILaunchButtonViewModel>
{
    public LaunchButtonView()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            var isRunning = ViewModel!.IsRunningObservable;
            var isNotRunning = ViewModel!.IsRunningObservable.Select(isRunning => !isRunning);
            isNotRunning.BindToUi(this, view => view.LaunchButton.IsVisible)
                .DisposeWith(d);

            isNotRunning.BindToUi(this, view => view.LaunchButton.IsEnabled)
                .DisposeWith(d);

            isRunning.BindToUi(this, view => view.ProgressBarControl.IsVisible)
                .DisposeWith(d);
            
            this.WhenAnyValue(view => view.ViewModel!.Command)
                .BindToUi(this, view => view.LaunchButton.Command)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.Progress)
                .Select(p => p == null)
                .BindToUi(this, view => view.ProgressBarControl.IsIndeterminate)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.Progress)
                .Where(p => p != null)
                .Select(p => p!.Value.Value)
                .BindToUi(this, view => view.ProgressBarControl.Value)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.Label)
                .BindToUi(this, view => view.LaunchText.Text)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.Label)
                .BindToUi(this, view => view.ProgressBarControl.ProgressTextFormat)
                .DisposeWith(d);
        });
    }
}

