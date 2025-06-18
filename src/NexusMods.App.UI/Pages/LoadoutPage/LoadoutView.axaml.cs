using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Resources;
using NexusMods.Collections;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.UI.Sdk.Icons;
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
            // initially hidden
            ContextControlGroup.IsVisible = false;
            
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
            
            this.OneWayBind(ViewModel, vm => vm.CollectionName, view => view.WritableCollectionPageHeader.Title)
                .AddTo(disposables);
            
            this.OneWayBind(ViewModel, vm => vm.RulesSectionViewModel, view => view.SortingSelectionView.ViewModel)
                .AddTo(disposables);

            this.OneWayBind(ViewModel, vm => vm.RulesSectionViewModel, view => view.SortingSelectionView.DataContext)
                .AddTo(disposables);
            
            this.OneWayBind(ViewModel, vm => vm.ItemCount, view => view.ModsCount.Text)
                .AddTo(disposables);
            
            this.OneWayBind(ViewModel, vm => vm.HasRulesSection, view => view.RulesTabItem.IsVisible)
                .AddTo(disposables);
            
            this.BindCommand(ViewModel, vm => vm.DeselectItemsCommand, view => view.DeselectItemsButton)
                .AddTo(disposables);

            this.BindCommand(ViewModel, vm => vm.CommandUploadRevision, view => view.ButtonUploadCollectionRevision)
                .AddTo(disposables);

            this.BindCommand(ViewModel, vm => vm.DeselectItemsCommand, view => view.DeselectItemsButton)
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

            
            this.WhenAnyValue( view => view.ViewModel!.SelectedSubTab)
                .WhereNotNull()
                .SubscribeWithErrorLogging(selectedSubTab =>
                {
                    RulesTabControl.SelectedItem = selectedSubTab switch
                    {
                        LoadoutPageSubTabs.Mods => ModsTabItem,
                        LoadoutPageSubTabs.Rules => RulesTabItem,
                        _ => throw new ArgumentOutOfRangeException(nameof(selectedSubTab), selectedSubTab, null)
                    };
                })
                .AddTo(disposables);
            
            this.WhenAnyValue(view => view.ViewModel!.SelectionCount)
                .WhereNotNull()
                .SubscribeWithErrorLogging(count =>
                    {
                        ContextControlGroup.IsVisible = count != 0;

                        if (count != 0)
                        {
                            DeselectItemsButton.Text = string.Format(Language.Library_DeselectItemsButton_Text, count);
                        }
                    }
                )
                .AddTo(disposables);
        });
    }

}

