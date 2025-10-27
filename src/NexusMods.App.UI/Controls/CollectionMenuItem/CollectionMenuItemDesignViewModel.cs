using NexusMods.UI.Sdk;
using NexusMods.UI.Sdk.Icons;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.CollectionMenuItem;

public class CollectionMenuItemDesignViewModel : AViewModel<ICollectionMenuItemViewModel>, ICollectionMenuItemViewModel
{
    public CollectionMenuItemDesignViewModel()
    {
        this.WhenAnyValue(x => x.CollectionType, x => x.IsAddedToTarget)
            .Subscribe(_ => UpdateRightIndicator());

        // Initialize with a sample local collection that's added
        CollectionName = "Local Collection";
        CollectionType = CollectionMenuItemType.Local;
        IsAddedToTarget = true;
        IsInstalled = false;

        UpdateRightIndicator();
    }

    [Reactive] public string CollectionName { get; set; } = "Sample Collection";
    [Reactive] public CollectionMenuItemType CollectionType { get; set; } = CollectionMenuItemType.Local;
    [Reactive] public bool IsAddedToTarget { get; set; } = false;
    [Reactive] public bool IsInstalled { get; set; } = false;
    [Reactive] public IconValue CollectionIcon { get; set; } = IconValues.CollectionsOutline;
    [Reactive] public IconValue? RightIndicatorIcon { get; set; } = null;
    [Reactive] public bool ShowRightIndicator { get; set; } = true;
    [Reactive] public bool? IsSelected { get; set; } = null;

    private void UpdateRightIndicator()
    {
        switch (CollectionType)
        {
            case CollectionMenuItemType.Local:
                RightIndicatorIcon = IsAddedToTarget ? IconValues.Check : null;
                ShowRightIndicator = IsAddedToTarget;
                break;
            case CollectionMenuItemType.NexusInstalled:
                RightIndicatorIcon = IconValues.Lock;
                ShowRightIndicator = true;
                break;
            case CollectionMenuItemType.NexusNotInstalled:
                RightIndicatorIcon = IconValues.Download;
                ShowRightIndicator = true;
                break;
        }
    }
}
