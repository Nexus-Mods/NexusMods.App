using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.MiniGameWidget.ComingSoon;

public partial class ComingSoonMiniGameWidget : ReactiveUserControl<IComingSoonMiniGameWidgetViewModel>
{
    public ComingSoonMiniGameWidget()
    {
        InitializeComponent();

        this.WhenActivated(d =>
            {
                this.BindCommand(ViewModel, vm => vm.ViewRoadmapCommand, view => view.ButtonViewRoadmap)
                    .DisposeWith(d);
            }
        );
    }
}
