using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using NexusMods.UI.Sdk.Icons;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

[UsedImplicitly]
public partial class AddPanelButtonView : ReactiveUserControl<IAddPanelButtonViewModel>
{
    public AddPanelButtonView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.BindCommand(ViewModel, vm => vm.AddPanelCommand, view => view.AddPanelButton)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.ButtonImage, view => view.ButtonImage.Value, img => new IconValue(new AvaloniaImage(img)))
                .DisposeWith(disposables);
        });
    }
}
