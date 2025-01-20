using Avalonia.Controls;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.Controls;
using NexusMods.MnemonicDB.Abstractions;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.LoadoutPage;

public partial class CollectionLoadoutView : ReactiveUserControl<ICollectionLoadoutViewModel>
{
    public CollectionLoadoutView()
    {
        InitializeComponent();

        TreeDataGridViewHelper.SetupTreeDataGridAdapter<CollectionLoadoutView, ICollectionLoadoutViewModel, LoadoutItemModel, EntityId>(this, TreeDataGrid, vm => vm.Adapter);

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.Adapter.Source.Value, view => view.TreeDataGrid.Source)
                .AddTo(disposables);
            this.OneWayBind(ViewModel, vm => vm.Name, view => view.CollectionName.Text)
                .AddTo(disposables);
            this.OneWayBind(ViewModel, vm => vm.TileImage, view => view.TileImage.Source)
                .AddTo(disposables);
            this.OneWayBind(ViewModel, vm => vm.BackgroundImage, view => view.BackgroundImage.Source)
                .AddTo(disposables);
            this.OneWayBind(ViewModel, vm => vm.AuthorAvatar, view => view.AuthorAvatar.Source)
                .AddTo(disposables);
            this.OneWayBind(ViewModel, vm => vm.RevisionNumber, view => view.Revision.Text)
                .AddTo(disposables);
            this.OneWayBind(ViewModel, vm => vm.AuthorName, view => view.AuthorName.Text)
                .AddTo(disposables);
            this.BindCommand(ViewModel, vm => vm.CommandToggle, view => view.CollectionToggle)
                .AddTo(disposables);
            this.OneWayBind(ViewModel, vm => vm.IsCollectionEnabled, view => view.CollectionToggle.IsChecked)
                .AddTo(disposables);
        });
    }
}
