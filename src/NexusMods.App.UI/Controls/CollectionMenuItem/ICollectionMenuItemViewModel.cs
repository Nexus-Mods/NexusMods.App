using NexusMods.UI.Sdk;
using NexusMods.UI.Sdk.Icons;

namespace NexusMods.App.UI.Controls.CollectionMenuItem;

public interface ICollectionMenuItemViewModel : IViewModelInterface
{
    string CollectionName { get; }
    CollectionMenuItemType CollectionType { get; }
    bool IsAddedToTarget { get; }
    bool IsInstalled { get; }
    IconValue CollectionIcon { get; }
    IconValue? RightIndicatorIcon { get; }
    bool ShowRightIndicator { get; }
}
