using NexusMods.UI.Sdk;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.CollectionMenuItem;

public class CollectionMenuItemDesignViewModel : AViewModel<ICollectionMenuItemViewModel>, ICollectionMenuItemViewModel
{
    [Reactive] public string CollectionName { get; set; } = "Vanilla Plus";
    [Reactive] public bool IsReadOnly { get; set; } = false;
    [Reactive] public string ToolTipText { get; set; } = "Read-only Nexus Mods Collection";
    [Reactive] public bool? IsSelected { get; set; } = true;
}
