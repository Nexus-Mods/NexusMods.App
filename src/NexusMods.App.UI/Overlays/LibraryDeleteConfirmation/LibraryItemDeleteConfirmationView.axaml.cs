using System.Reactive.Disposables;
using Avalonia.Markup.Xaml;
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
            
            // Hide the individual item sections.
            this.OneWayBind(ViewModel, 
                vm => vm.WarningDetector.NonPermanentItems.Count, 
                v => v.NonPermanentWarningPanel.IsVisible, 
                count => count > 0)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, 
                vm => vm.WarningDetector.ManuallyAddedItems.Count, 
                v => v.ManuallyAddedWarningPanel.IsVisible, 
                count => count > 0)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, 
                vm => vm.WarningDetector.ItemsInLoadouts.Count, 
                v => v.LoadoutWarningPanel.IsVisible, 
                count => count > 0)
                .DisposeWith(d);

            // Bind item lists
            this.OneWayBind(ViewModel, 
                vm => vm.WarningDetector.NonPermanentItems, 
                v => v.NonPermanentItemsList.ItemsSource)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, 
                vm => vm.WarningDetector.ManuallyAddedItems, 
                v => v.ManuallyAddedItemsList.ItemsSource)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, 
                vm => vm.WarningDetector.ItemsInLoadouts, 
                v => v.LoadoutItemsList.ItemsSource)
                .DisposeWith(d);

            // Bind button commands
            YesButton.Command = ReactiveCommand.Create(() => ViewModel!.Complete(true));
            NoButton.Command = ReactiveCommand.Create(() => ViewModel!.Complete(false));
            CloseButton.Command = ReactiveCommand.Create(() => ViewModel!.Complete(false));
        });
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
