using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.ReactiveUI;
using NexusMods.Paths;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.ModContent;

[ExcludeFromCodeCoverage]
public partial class ModContentView : ReactiveUserControl<IModContentViewModel>
{
    public ModContentView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            // Populate the tree
            this.OneWayBind(ViewModel, vm => vm.Tree, view => view.ModContentTreeDataGrid.Source!)
                .DisposeWith(disposables);

            // Disable the view when user is Create a new folder
            this.OneWayBind(ViewModel, vm => vm.IsDisabled, view => view.TopLevelPanel.IsEnabled,
                    isDisabled => !isDisabled)
                .DisposeWith(disposables);

            // A hack around https://github.com/AvaloniaUI/Avalonia.Controls.TreeDataGrid/issues/221
            ModContentTreeDataGrid.Width = double.NaN;
        });
    }
}
