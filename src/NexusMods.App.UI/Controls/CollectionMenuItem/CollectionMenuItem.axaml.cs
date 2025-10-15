using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.ReactiveUI;
using NexusMods.UI.Sdk.Icons;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.CollectionMenuItem;

[PseudoClasses(":local", ":nexus-installed", ":nexus-not-installed", ":added", ":not-added")]
public partial class CollectionMenuItem : ReactiveUserControl<ICollectionMenuItemViewModel>
{
    public CollectionMenuItem()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(x => x.ViewModel!.CollectionType)
                .OnUI()
                .Subscribe(SetCollectionTypePseudoClasses)
                .DisposeWith(d);

            this.WhenAnyValue(x => x.ViewModel!.IsAddedToTarget)
                .OnUI()
                .Subscribe(SetAddedPseudoClass)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.CollectionName, v => v.CollectionName.Text)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.CollectionIcon, v => v.CollectionIcon.Value)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.RightIndicatorIcon, v => v.RightIndicatorIcon.Value)
                .DisposeWith(d);

            this.OneWayBind(ViewModel, vm => vm.ShowRightIndicator, v => v.RightIndicatorIcon.IsVisible)
                .DisposeWith(d);
        });
    }

    private void SetCollectionTypePseudoClasses(CollectionMenuItemType type)
    {
        PseudoClasses.Set(":local", type == CollectionMenuItemType.Local);
        PseudoClasses.Set(":nexus-installed", type == CollectionMenuItemType.NexusInstalled);
        PseudoClasses.Set(":nexus-not-installed", type == CollectionMenuItemType.NexusNotInstalled);
    }

    private void SetAddedPseudoClass(bool isAdded)
    {
        PseudoClasses.Set(":added", isAdded);
        PseudoClasses.Set(":not-added", !isAdded);
    }
}
