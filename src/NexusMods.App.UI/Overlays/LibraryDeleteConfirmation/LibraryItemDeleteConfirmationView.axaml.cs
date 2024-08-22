using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.Resources;
using ReactiveUI;

namespace NexusMods.App.UI.Overlays.LibraryDeleteConfirmation;

public partial class LibraryItemDeleteConfirmationView : ReactiveUserControl<ILibraryItemDeleteConfirmationViewModel>
{
    public LibraryItemDeleteConfirmationView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            HeadingText.Text = Language.LibraryItemDeleteConfirmation_Title;

            // Bind item lists
            this.OneWayBind(ViewModel,
                vm => vm.AllItems,
                v => v.RemovedItemsList.ItemsSource)
                .DisposeWith(d);
            
            // Show/hide the loadouts section.
            this.WhenAnyValue(view => view.ViewModel!.LoadoutsUsed)
                .Subscribe(loadoutsUsed =>
                    {
                        var visible = loadoutsUsed.Count > 0;
                        LoadoutsPanel.IsVisible = visible;
                    }
                )
                .DisposeWith(d);

            // Show loadouts section
            this.OneWayBind(ViewModel,
                    vm => vm.LoadoutsUsed,
                    v => v.SourceLoadoutsList.ItemsSource)
                .DisposeWith(d);

            // Bind button commands
            DeleteButton.Command = ReactiveCommand.Create(() => ViewModel!.Complete(true));
            NoButton.Command = ReactiveCommand.Create(() => ViewModel!.Complete(false));
            CloseButton.Command = ReactiveCommand.Create(() => ViewModel!.Complete(false));
        });
    }
}
