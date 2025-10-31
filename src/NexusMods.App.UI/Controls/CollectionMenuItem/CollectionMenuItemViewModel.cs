using NexusMods.UI.Sdk;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.CollectionMenuItem;

public class CollectionMenuItemViewModel : AViewModel<ICollectionMenuItemViewModel>, ICollectionMenuItemViewModel
{
    [Reactive] public string CollectionName { get; set; } = string.Empty;
    [Reactive] public bool IsReadOnly { get; set; }
    [Reactive] public string ToolTipText { get; set; } = string.Empty;
    [Reactive] public bool? IsSelected { get; set; } = null;
}
