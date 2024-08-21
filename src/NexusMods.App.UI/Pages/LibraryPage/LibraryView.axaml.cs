using Avalonia.Controls;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using NexusMods.App.UI.Controls;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.LibraryPage;

[UsedImplicitly]
public partial class LibraryView : ReactiveUserControl<ILibraryViewModel>
{
    public LibraryView()
    {
        InitializeComponent();

        TreeDataGrid.ElementFactory = new CustomElementFactory();

        this.WhenActivated(disposables =>
        {
            var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
            if (storageProvider is not null)
            {
                this.WhenAnyValue(view => view.ViewModel)
                    .WhereNotNull()
                    .SubscribeWithErrorLogging(vm => vm.StorageProvider = storageProvider)
                    .AddTo(disposables);
            }

            this.BindCommand(ViewModel, vm => vm.SwitchViewCommand, view => view.SwitchView)
                .AddTo(disposables);

            this.BindCommand(ViewModel, vm => vm.InstallSelectedItemsCommand, view => view.AddModButton)
                .AddTo(disposables);

            this.BindCommand(ViewModel, vm => vm.InstallSelectedItemsWithAdvancedInstallerCommand, view => view.AddModAdvancedButton)
                .AddTo(disposables);

            this.BindCommand(ViewModel, vm => vm.OpenFilePickerCommand, view => view.GetModsFromDriveButton)
                .AddTo(disposables);

            this.BindCommand(ViewModel, vm => vm.OpenNexusModsCommand, view => view.GetModsFromNexusButton)
                .AddTo(disposables);

            var activate = Observable.FromEventHandler<TreeDataGridRowEventArgs>(
                addHandler: handler => TreeDataGrid.RowPrepared += handler,
                removeHandler: handler => TreeDataGrid.RowPrepared -= handler
            ).Select(static tuple => (tuple.e.Row.Model, true));

            var deactivate = Observable.FromEventHandler<TreeDataGridRowEventArgs>(
                addHandler: handler => TreeDataGrid.RowClearing += handler,
                removeHandler: handler => TreeDataGrid.RowClearing -= handler
            ).Select(static tuple => (tuple.e.Row.Model, false));

            deactivate.Merge(activate)
                .Where(static tuple => tuple.Model is LibraryItemModel)
                .Select(static tuple => ((tuple.Model as LibraryItemModel)!, tuple.Item2))
                .Subscribe(this, static (tuple, view) => view.ViewModel!.ActivationSubject.OnNext(tuple))
                .AddTo(disposables);

            this.OneWayBind(ViewModel, vm => vm.Source, view => view.TreeDataGrid.Source)
                .AddTo(disposables);

            this.OneWayBind(ViewModel, vm => vm.IsEmpty, view => view.EmptyState.IsActive)
                .AddTo(disposables);

            this.OneWayBind(ViewModel, vm => vm.EmptyLibrarySubtitleText, view => view.EmptyLibrarySubtitleTextBlock.Text)
                .AddTo(disposables);

            this.BindCommand(ViewModel, vm => vm.OpenNexusModsCommand, view => view.EmptyLibraryLinkButton)
                .AddTo(disposables);
        });
    }
}
