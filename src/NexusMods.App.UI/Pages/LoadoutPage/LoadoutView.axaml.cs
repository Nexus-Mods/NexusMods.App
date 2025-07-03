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

                var isCollection = vm?.IsCollection ?? false;
                view.AllPageHeader.IsVisible = !isCollection;
                view.Statusbar.IsVisible = isCollection;
                view.ButtonUploadCollectionRevision.IsVisible = isCollection && CollectionCreator.IsFeatureEnabled;
                view.WritableCollectionPageHeader.IsVisible = isCollection;

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

            this.OneWayR3Bind(static view => view.BindableViewModel, static vm => vm.CollectionName, static (view, collectionName) => view.WritableCollectionPageHeader.Title = collectionName)
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

            this.ObserveViewModelProperty(static view => view.BindableViewModel, static vm => vm.IsCollectionUploaded)
                .Subscribe(this, static (isCollectionUploaded, self) =>
                {
                    self.StatusText.Text = isCollectionUploaded ? "Uploaded" : "Not Uploaded";
                    self.ButtonUploadCollectionRevision.Text = isCollectionUploaded ? "Upload update" : "Share";
                    self.ButtonUploadCollectionRevision.ShowIcon = isCollectionUploaded ? StandardButton.ShowIconOptions.None : StandardButton.ShowIconOptions.Left;
                    self.ButtonOpenRevisionUrl.IsVisible = isCollectionUploaded;

                    if (isCollectionUploaded)
                    {
                        self.StatusText.Classes.Add("Success");
                        self.StatusIcon.Classes.Add("Success");
                        self.StatusIcon.Value = IconValues.CollectionsOutline;
                    }
                    else
                    {
                        self.StatusText.Classes.Remove("Success");
                        self.StatusIcon.Classes.Remove("Success");
                        self.StatusIcon.Value = IconValues.Info;
                    }

                    self.StatusText.Text = isCollectionUploaded ? "Uploaded" : "Not Uploaded";
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

