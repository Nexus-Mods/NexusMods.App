using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Left;

[ExcludeFromCodeCoverage]
public partial class ModContentView : ReactiveUserControl<IModContentViewModel>
{
    public ModContentView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.Tree, view => view.ModContentTreeDataGrid.Source!)
                .DisposeWith(disposables);
        });
    }
}
