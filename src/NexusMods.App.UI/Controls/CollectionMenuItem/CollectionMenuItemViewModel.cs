using NexusMods.UI.Sdk;
using NexusMods.UI.Sdk.Icons;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.CollectionMenuItem;

public class CollectionMenuItemViewModel : AViewModel<ICollectionMenuItemViewModel>, ICollectionMenuItemViewModel
{
    [Reactive] public string CollectionName { get; set; } = string.Empty;
    [Reactive] public CollectionMenuItemType CollectionType { get; set; }
    [Reactive] public bool IsAddedToTarget { get; set; }
    [Reactive] public IconValue CollectionIcon { get; set; } = IconValues.CollectionsOutline;
    [Reactive] public IconValue? RightIndicatorIcon { get; set; }
    [Reactive] public bool ShowRightIndicator { get; set; } = true;
    [Reactive] public bool? IsSelected { get; set; } = null;

    public CollectionMenuItemViewModel()
    {
        this.WhenAnyValue(x => x.CollectionType, x => x.IsAddedToTarget)
            .Subscribe(_ => UpdateRightIndicator());
    }

    public CollectionMenuItemViewModel(string name, CollectionMenuItemType type, bool isAdded)
    {
        CollectionName = name;
        CollectionType = type;
        IsAddedToTarget = isAdded;

        this.WhenAnyValue(x => x.CollectionType, x => x.IsAddedToTarget)
            .Subscribe(_ => UpdateRightIndicator());

        UpdateRightIndicator();
    }

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
