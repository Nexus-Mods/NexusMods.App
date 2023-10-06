using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using NexusMods.App.UI;
using NexusMods.Games.AdvancedInstaller.UI.Content.Left;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.Content;

public partial class BodyView : ReactiveUserControl<IBodyViewModel>
{
    public BodyView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind<IBodyViewModel, BodyView, IModContentViewModel, object>(ViewModel,
                    vm => vm.ModContentViewModel, view => view.ModContentSectionViewHost.ViewModel!)
                .DisposeWith(disposables);

            this.OneWayBind<IBodyViewModel, BodyView, IViewModel, object>(ViewModel, vm => vm.CurrentPreviewViewModel,
                    view => view.PreviewSectionViewHost.ViewModel!)
                .DisposeWith(disposables);
        });
    }
}
