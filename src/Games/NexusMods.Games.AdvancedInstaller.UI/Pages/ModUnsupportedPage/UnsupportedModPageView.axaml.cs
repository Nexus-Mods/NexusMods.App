using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI;

[ExcludeFromCodeCoverage]
public partial class UnsupportedModPageView : ReactiveUserControl<IUnsupportedModPageViewModel>
{
    public UnsupportedModPageView()
    {
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(view => view.ViewModel)
                .WhereNotNull()
                .Do(vm => ModNameTextBlock.Text = vm.ModName)
                .Subscribe()
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.AcceptCommand,
                    view => view.InstallButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.DeclineCommand,
                    view => view.CancelButton)
                .DisposeWith(d);
        });
        InitializeComponent();
    }
}
