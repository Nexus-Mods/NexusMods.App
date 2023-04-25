using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.Icons;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Items;

public partial class LaunchButtonView : ReactiveUserControl<ILaunchButtonViewModel>
{
    public LaunchButtonView()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            var isRunning =
                this.WhenAnyValue(view => view.ViewModel!.IsRunning);

            this.WhenAnyValue(view => view.ViewModel!.IsEnabled)
                .BindToUi(this, view => view.LaunchButton.IsEnabled)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.Command)
                .BindToUi(this, view => view.LaunchButton.Command)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.Command)
                .SelectMany(cmd => cmd.IsExecuting)
                .CombineLatest(isRunning)
                .Select(ex => ex is { First: false, Second: false })
                .BindToUi(this, view => view.LaunchButton.IsVisible)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.Command)
                .SelectMany(cmd => cmd.IsExecuting)
                .CombineLatest(isRunning)
                .Select(ex => ex.First || ex.Second)
                .BindToUi(this, view => view.ProgressBarControl.IsVisible)
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

