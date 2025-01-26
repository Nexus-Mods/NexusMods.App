using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Extensions;
using NexusMods.MnemonicDB.Abstractions;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.LoadoutPage;

[UsedImplicitly]
public partial class LoadoutView : ReactiveUserControl<ILoadoutViewModel>
{
    public LoadoutView()
    {
        InitializeComponent();

        TreeDataGridViewHelper.SetupTreeDataGridAdapter<LoadoutView, ILoadoutViewModel, CompositeItemModel<EntityId>, EntityId>(this, TreeDataGrid, vm => vm.Adapter);

        this.WhenActivated(disposables =>
        {
            this.BindCommand(ViewModel, vm => vm.SwitchViewCommand, view => view.SwitchView)
                .AddTo(disposables);

            this.BindCommand(ViewModel, vm => vm.ViewFilesCommand, view => view.ViewFilesButton)
                .AddTo(disposables);
            
            this.BindCommand(ViewModel, vm => vm.ViewLibraryCommand, view => view.ViewLibraryButton)
                .AddTo(disposables);

            this.BindCommand(ViewModel, vm => vm.RemoveItemCommand, view => view.DeleteButton)
                .AddTo(disposables);

            this.OneWayBind(ViewModel, vm => vm.Adapter.Source.Value, view => view.TreeDataGrid.Source)
                .AddTo(disposables);

            this.OneWayBind(ViewModel, vm => vm.Adapter.IsSourceEmpty.Value, view => view.EmptyState.IsActive)
                .AddTo(disposables);
            
            this.OneWayBind(ViewModel, vm => vm.EmptyStateTitleText, view => view.EmptyState.Header)
                .AddTo(disposables);
            
            this.OneWayBind(ViewModel, vm => vm.IsCollection, view => view.CollectionToolbar.IsVisible)
                .AddTo(disposables);
            
            this.BindCommand(ViewModel, vm => vm.CollectionToggleCommand, view => view.CollectionToggle)
                .AddTo(disposables);
            
            this.WhenAnyValue(view => view.ViewModel!.IsCollectionEnabled)
                .WhereNotNull()
                .SubscribeWithErrorLogging(isEnabled =>
                {
                    CollectionToggle.IsChecked = isEnabled;
                    ToolbarEnabled.IsVisible = isEnabled;
                    ToolbarDisabled.IsVisible = !isEnabled;
                    
                    CollectionToolbar.Classes.ToggleIf("Warning", !isEnabled);
                })
                .AddTo(disposables);
        });
    }
}

