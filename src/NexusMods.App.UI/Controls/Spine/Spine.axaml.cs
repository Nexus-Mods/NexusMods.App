using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Spine;

public partial class Spine : ReactiveUserControl<ISpineViewModel>
{
    public Spine()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.Home, v => v.Home.ViewModel)
                .DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.Games, v => v.Games.Items)
                .DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.Add, v => v.Add.ViewModel)
                .DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.Downloads, v => v.Download.ViewModel)
                .DisposeWith(disposables);
        });
    }
}
