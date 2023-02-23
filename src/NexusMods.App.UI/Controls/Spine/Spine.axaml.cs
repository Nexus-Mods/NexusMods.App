using Avalonia.Controls.Mixins;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Spine;

public partial class Spine : ReactiveUserControl<SpineViewModel>
{
    public Spine()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.Games, v => v.Games.Items)
                .DisposeWith(disposables);
        });
    }
}