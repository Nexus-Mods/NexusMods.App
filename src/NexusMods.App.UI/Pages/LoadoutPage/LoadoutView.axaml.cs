using AvaloniaEdit.Rendering;
using JetBrains.Annotations;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Resources;
using NexusMods.Collections;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.UI.Sdk;
using NexusMods.UI.Sdk.Icons;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.LoadoutPage;

[UsedImplicitly]
public partial class LoadoutView : R3UserControl<ILoadoutViewModel>
{
    public LoadoutView()
    {
        InitializeComponent();

        TreeDataGridViewHelper.SetupTreeDataGridAdapter<LoadoutView, ILoadoutViewModel, CompositeItemModel<EntityId>, EntityId>(this, TreeDataGrid, vm => vm.Adapter);

        this.WhenActivated(disposables =>
        {
            // initially hidden
            ContextControlGroup.IsVisible = false;

            BindableViewModel.Subscribe(this, static (vm, view) =>
            {
                view.EmptyState.Header = vm?.EmptyStateTitleText ?? string.Empty;
                view.SortingSelectionView.ViewModel = vm?.RulesSectionViewModel;
                view.RulesTabItem.IsVisible = vm?.HasRulesSection ?? false;

                var selectedSubTab = vm?.SelectedSubTab;
                if (selectedSubTab is not null)
                {
                    view.RulesTabControl.SelectedItem = selectedSubTab switch
                    {
                        LoadoutPageSubTabs.Mods => view.ModsTabItem,
                        LoadoutPageSubTabs.Rules => view.RulesTabItem,
                        _ => throw new ArgumentOutOfRangeException(nameof(selectedSubTab), selectedSubTab, null)
                    };
                }
            }).AddTo(disposables);

            this.BindCommand(ViewModel, vm => vm.CommandOpenFilesPage, view => view.ViewFilesButton)
                .AddTo(disposables);
            
            this.BindCommand(ViewModel, vm => vm.CommandOpenLibraryPage, view => view.ViewLibraryButton)
                .AddTo(disposables);

            this.BindCommand(ViewModel, vm => vm.CommandRemoveItem, view => view.DeleteButton)
                .AddTo(disposables);

            this.OneWayBind(ViewModel, vm => vm.Adapter.Source.Value, view => view.TreeDataGrid.Source)
                .AddTo(disposables);

            this.OneWayBind(ViewModel, vm => vm.Adapter.IsSourceEmpty.Value, view => view.EmptyState.IsActive)
                .AddTo(disposables);

            this.OneWayBind(ViewModel, vm => vm.CollectionName, view => view.WritableCollectionPageHeader.Title)
                .AddTo(disposables);

            this.OneWayR3Bind(static view => view.BindableViewModel, static vm => vm.ItemCount, static (view, count) => view.ModsCount.Text = count.ToString())
                .AddTo(disposables);

            this.BindCommand(ViewModel, vm => vm.CommandDeselectItems, view => view.DeselectItemsButton)
                .AddTo(disposables);

            this.BindCommand(ViewModel, vm => vm.CommandUploadRevision, view => view.ButtonUploadCollectionRevision)
                .AddTo(disposables);

            this.BindCommand(ViewModel, vm => vm.CommandOpenRevisionUrl, view => view.ButtonOpenRevisionUrl)
                .AddTo(disposables);

            this.BindCommand(ViewModel, vm => vm.CommandRenameGroup, view => view.MenuItemRenameCollection)
                .AddTo(disposables);

            this.WhenAnyValue(view => view.ViewModel!.IsCollection)
                .WhereNotNull()
                .SubscribeWithErrorLogging(isCollection =>
                {
                    ButtonUploadCollectionRevision.IsVisible = isCollection && CollectionCreator.IsFeatureEnabled;
                    WritableCollectionPageHeader.IsVisible = isCollection;
                    AllPageHeader.IsVisible = !isCollection;
                })
                .AddTo(disposables);
            
            this.WhenAnyValue(view => view.ViewModel!.IsCollectionUploaded)
                .WhereNotNull()
                .SubscribeWithErrorLogging(isCollectionUploaded =>
                {
                    StatusText.Text = isCollectionUploaded ? "Uploaded" : "Not Uploaded";
                    ButtonUploadCollectionRevision.Text = isCollectionUploaded ? "Upload update" : "Share";
                    ButtonUploadCollectionRevision.ShowIcon = isCollectionUploaded ? StandardButton.ShowIconOptions.None : StandardButton.ShowIconOptions.Left;
                    ButtonOpenRevisionUrl.IsVisible = isCollectionUploaded;
                    
                    if (isCollectionUploaded)
                    {
                        StatusText.Classes.Add("Success");
                        StatusIcon.Classes.Add("Success");
                        StatusIcon.Value = IconValues.CollectionsOutline;
                    }
                    else
                    {
                        StatusText.Classes.Remove("Success");
                        StatusIcon.Classes.Remove("Success");
                        StatusIcon.Value = IconValues.Info;
                    }
                    
                    StatusText.Text = isCollectionUploaded ? "Uploaded" : "Not Uploaded";
                })
                .AddTo(disposables);

            this.ObserveViewModelProperty(static view => view.BindableViewModel, static vm => vm.SelectionCount)
                .Subscribe(this, static (count, self) =>
                {
                    self.ContextControlGroup.IsVisible = count != 0;
                    self.DeselectItemsButton.Text = count == 0 ? string.Empty : string.Format(Language.Library_DeselectItemsButton_Text, count);
                })
                .AddTo(disposables);
        });
    }

}

