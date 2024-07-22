using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Resources;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.LoadoutCard;

public partial class LoadoutCardView : ReactiveUserControl<ILoadoutCardViewModel>
{
    public LoadoutCardView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
            {
                // LoadoutBadge
                this.OneWayBind(ViewModel,
                        vm => vm.LoadoutBadgeViewModel,
                        view => view.LoadoutBadge.ViewModel)
                    .DisposeWith(d);
                
                // Game Image
                this.OneWayBind(ViewModel,
                        vm => vm.LoadoutImage,
                        view => view.GameImage.Source)
                    .DisposeWith(d);
                
                // Loadout Name
                this.OneWayBind(ViewModel,
                        vm => vm.LoadoutName,
                        view => view.LoadoutNameTextBlock.Text)
                    .DisposeWith(d);
                
                // Currently applied visibility
                this.OneWayBind(ViewModel,
                        vm => vm.IsLoadoutApplied,
                        view => view.CurrentlyAppliedFlexPanel.IsVisible)
                    .DisposeWith(d);
                
                // Last applied visibility
                this.WhenAnyValue(view => view.ViewModel!.IsLoadoutApplied)
                    .Select(isApplied => !isApplied)
                    .OnUI()
                    .BindToView(this, view => view.LastAppliedTimeTextBlock.IsVisible)
                    .DisposeWith(d);

                // Last applied time
                this.OneWayBind(ViewModel,
                        vm => vm.HumanizedLoadoutLastApplyTime,
                        view => view.LastAppliedTimeTextBlock.Text)
                    .DisposeWith(d);
                
                // Created time
                this.OneWayBind(ViewModel,
                        vm => vm.HumanizedLoadoutCreationTime,
                        view => view.CreatedTimeTextBlock.Text)
                    .DisposeWith(d);
                
                // Mod count
                this.OneWayBind(ViewModel,
                        vm => vm.LoadoutModCount,
                        view => view.NumberOfModsTextBlock.Text)
                    .DisposeWith(d);
                
                // Deleting state
                this.WhenAnyValue(view => view.ViewModel!.IsDeleting)
                    .OnUI()
                    .Subscribe(isDeleting =>
                    {
                        if (isDeleting) 
                            OverlayTextBlock.Text = Language.LoadoutCardViewDeletingText;
                        IsEnabled = !isDeleting;
                        OverlayFlexPanel.IsVisible = isDeleting;
                        ActionsBorder.IsVisible = !isDeleting;
                    })
                    .DisposeWith(d);
                
                // Skeleton Creating state
                this.WhenAnyValue(view => view.ViewModel!.IsSkeleton)
                    .OnUI()
                    .Subscribe(isSkeleton =>
                    {
                        if (isSkeleton) 
                            OverlayTextBlock.Text = Language.LoadoutCardViewCreatingText;
                        IsEnabled = !isSkeleton;
                        OverlayFlexPanel.IsVisible = isSkeleton;
                        BodyAndActionsGroupFlexPanel.IsVisible = !isSkeleton;
                    })
                    .DisposeWith(d);

                // Clone loadout command
                this.BindCommand(ViewModel,
                        vm => vm.CloneLoadoutCommand,
                        view => view.CreateCopyButton)
                    .DisposeWith(d);
                
                // Delete loadout command
                this.BindCommand(ViewModel,
                        vm => vm.DeleteLoadoutCommand,
                        view => view.DeleteButton)
                    .DisposeWith(d);
                
                // Visit loadout command
                this.BindCommand(ViewModel,
                        vm => vm.VisitLoadoutCommand,
                        view => view.CardOuterButton)
                    .DisposeWith(d);
            }
        );
    }
}

