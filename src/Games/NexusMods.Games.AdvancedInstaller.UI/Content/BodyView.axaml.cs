using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using NexusMods.Games.AdvancedInstaller.UI.Content.Left;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.Content;

[ExcludeFromCodeCoverage]
public partial class BodyView : ReactiveUserControl<IBodyViewModel>
{
    public BodyView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.ModContentViewModel, view => view.ModContentSectionViewHost.ViewModel!)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.CurrentPreviewViewModel, view => view.PreviewSectionViewHost.ViewModel!)
                .DisposeWith(disposables);
        });
    }
}
