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
            var isNotRunning = ViewModel!.IsRunningObservable.Select(running => !running);

            // Hide launch button when running
            isNotRunning.BindToUi(this, view => view.LaunchButton.IsVisible)
                .DisposeWith(d);

            isNotRunning.BindToUi(this, view => view.LaunchButton.IsEnabled)
                .DisposeWith(d);

            // Show progress bar when running
            isRunning.BindToUi(this, view => view.ProgressBarControl.IsVisible)
                .DisposeWith(d);
            
            // Bind the 'launch' button.
            this.WhenAnyValue(view => view.ViewModel!.Command)
                .BindToUi(this, view => view.LaunchButton.Command)
                .DisposeWith(d);

            // Bind the progress to the progress bar.
            this.WhenAnyValue(view => view.ViewModel!.Progress)
                .Select(p => p == null)
                .BindToUi(this, view => view.ProgressBarControl.IsIndeterminate)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.Progress)
                .Where(p => p != null)
                .Select(p => p!.Value.Value)
                .BindToUi(this, view => view.ProgressBarControl.Value)
                .DisposeWith(d);

            // Set the 'play' / 'running' text.
            this.WhenAnyValue(view => view.ViewModel!.Label)
                .BindToUi(this, view => view.LaunchText.Text)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.Label)
                .BindToUi(this, view => view.ProgressBarControl.ProgressTextFormat)
                .DisposeWith(d);
        });
    }
}

