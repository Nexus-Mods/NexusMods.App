using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using DynamicData.Binding;
using Humanizer;
using Humanizer.Localisation;
using JetBrains.Annotations;
using NexusMods.Abstractions.NexusModsLibrary.Models;
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

                        var isCollection = vm?.IsCollection ?? true;
                        
                        view.AllPageHeader.IsVisible = !isCollection;
                        view.Statusbar.IsVisible = isCollection;
                        
                        view.ButtonShareCollection.IsVisible = isCollection && CollectionCreator.IsFeatureEnabled;
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
                    }
                ).AddTo(disposables);

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
                
                this.OneWayR3Bind(static view => view.BindableViewModel, 
                        static vm => vm.CollectionStatus, 
                        static (view, status) =>
                        {
                            view.ButtonChangeVisibility.LeftIcon = status switch
                            {
                                CollectionStatus.Unlisted => IconValues.VisibilityUnlisted,
                                CollectionStatus.Listed => IconValues.VisibilityListed
                            };
                        })
                    .AddTo(disposables);
                
                this.OneWayR3Bind(static view => view.BindableViewModel, 
                        static vm => vm.LastUploadedDate, 
                        static (view, lastUploaded) =>
                        {
                            view.LastUploadedText.Text = $"{(DateTimeOffset.Now - lastUploaded).Humanize()} ago";
                            ToolTip.SetTip(view.LastUploadedText, lastUploaded.ToString("F"));
                        }
                    )
                    .AddTo(disposables);
                
                this.OneWayR3Bind(static view => view.BindableViewModel, 
                        static vm => vm.RevisionNumber, 
                        static (view, revision) => view.RevisionText.Text = $"Revision {revision.ToString()}")
                    .AddTo(disposables);

                this.BindCommand(ViewModel, vm => vm.CommandDeselectItems, view => view.DeselectItemsButton)
                    .AddTo(disposables);

                this.BindCommand(ViewModel, vm => vm.CommandShareCollection, view => view.ButtonShareCollection)
                    .AddTo(disposables);
                
                this.BindCommand(ViewModel, vm => vm.CommandUploadAndPublishRevision, view => view.SplitButtonPublishCollection)
                    .AddTo(disposables);
                
                this.BindCommand(ViewModel, vm => vm.CommandUploadDraftRevision, view => view.MenuItemUploadDraft)
                    .AddTo(disposables);
                
                this.BindCommand(ViewModel, vm => vm.CommandOpenRevisionUrl, view => view.ButtonOpenRevisionUrl)
                    .AddTo(disposables);

                this.BindCommand(ViewModel, vm => vm.CommandRenameGroup, view => view.MenuItemRenameCollection)
                    .AddTo(disposables);
                
                this.BindCommand(ViewModel, vm => vm.CommandChangeVisibility, view => view.ButtonChangeVisibility)
                    .AddTo(disposables);
                
                this.ObserveViewModelProperty(static view => view.BindableViewModel, 
                        static vm => vm.IsCollectionUploaded)
                    .Subscribe(this, static (isCollectionUploaded, self) =>
                        {
                            self.ButtonShareCollection.IsVisible = !isCollectionUploaded;
                            self.SplitButtonPublishCollection.IsVisible = isCollectionUploaded;
                            self.VisibilityButtonStack.IsVisible = isCollectionUploaded;
                            self.IsUploadedStack.IsVisible = isCollectionUploaded;
                        }
                    ).AddTo(disposables);
                
                this.ObserveViewModelProperty(static view => view.BindableViewModel, static vm => vm.SelectionCount)
                    .Subscribe(this, static (count, self) =>
                        {
                            self.ContextControlGroup.IsVisible = count != 0;
                            self.DeselectItemsButton.Text = count == 0 ? string.Empty : string.Format(Language.Library_DeselectItemsButton_Text, count);
                        }
                    )
                    .AddTo(disposables);

                this.ObserveViewModelProperty(static view => view.BindableViewModel, static vm => vm.Adapter.IsSourceEmpty)
                    .Subscribe(this,
                        static (b, view) =>
                        {
                            ToolTip.SetTip(view.ButtonShareCollection, b ? "You can't share this collection until it has at least one installed mod." : null);
                        }
                    )
                    .AddTo(disposables);
            }
        );
    }
    
    private static void ShowTooltipFor(Control target, string text, TimeSpan duration)
    {
        // get current tip
        var tip = ToolTip.GetTip(target);
        
        // set the new tip
        ToolTip.SetTip(target, text);
        
        // force open it
        ToolTip.SetIsOpen(target, true);  // :contentReference[oaicite:0]{index=0}

        // schedule its closure
        var timer = new DispatcherTimer
        {
            Interval = duration
        };
        
        // stop the timer and close it when it ticks
        timer.Tick += (s, e) =>
        {
            timer.Stop();
            ToolTip.SetIsOpen(target, false);
            ToolTip.SetTip(target, tip); // restore the original tip
        };
        
        timer.Start();
    }

    private void ButtonOpenRevisionUrl_OnClick(object? sender, RoutedEventArgs e)
    {
        ShowTooltipFor(ButtonOpenRevisionUrl, "Link copied", TimeSpan.FromSeconds(2));
    }
}
