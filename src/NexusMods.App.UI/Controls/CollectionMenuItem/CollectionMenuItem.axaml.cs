using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.CollectionMenuItem;

public partial class CollectionMenuItem : ReactiveUserControl<ICollectionMenuItemViewModel>
{
    public CollectionMenuItem()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.OneWayBind(ViewModel, vm => vm.CollectionName, v => v.CollectionName.Text)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.IsReadOnly, v => v.LockIcon.IsVisible)
                .DisposeWith(d);

            this.Bind(ViewModel, vm => vm.IsSelected, v => v.SelectionCheckbox.IsChecked)
                .DisposeWith(d);

            // Disable controls when IsReadOnly is true
            this.WhenAnyValue(x => x.ViewModel!.IsReadOnly)
                .Subscribe(isReadOnly =>
                {
                    CollectionName.IsEnabled = !isReadOnly;
                    SelectionCheckbox.IsEnabled = !isReadOnly;
                })
                .DisposeWith(d);
        });
    }
}
