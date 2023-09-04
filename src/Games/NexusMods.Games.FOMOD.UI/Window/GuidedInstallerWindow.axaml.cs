using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.ReactiveUI;
using NexusMods.App.UI;
using ReactiveUI;

namespace NexusMods.Games.FOMOD.UI;

public partial class GuidedInstallerWindow : ReactiveWindow<IGuidedInstallerWindowViewModel>
{
    public GuidedInstallerWindow()
    {
        InitializeComponent();

#if DEBUG
        this.AttachDevTools();
#endif

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.WindowName, view => view.Title)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.ActiveStepViewModel, view => view.StepViewHost.ViewModel)
                .DisposeWith(disposables);

            this.WhenAnyValue(view => view.ViewModel!.CloseCommand.IsExecuting)
                .SelectMany(e => e)
                .Where(e => e)
                .SubscribeWithErrorLogging(logger: default, _ => Close())
                .DisposeWith(disposables);
        });
    }
}

