using NexusMods.UI.Sdk;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.CollectionMenuItem;

public class CollectionMenuItemDesignViewModel : AViewModel<ICollectionMenuItemViewModel>, ICollectionMenuItemViewModel
{
    [Reactive] public string CollectionName { get; set; } = "Sample Collection";
    [Reactive] public bool IsReadOnly { get; set; } = false;
    [Reactive] public bool? IsSelected { get; set; } = true;
}
