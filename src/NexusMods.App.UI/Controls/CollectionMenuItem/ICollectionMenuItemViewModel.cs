using NexusMods.UI.Sdk;

namespace NexusMods.App.UI.Controls.CollectionMenuItem;

public interface ICollectionMenuItemViewModel : IViewModelInterface
{
    string CollectionName { get; set; }
    bool IsReadOnly { get; set; }
    
    /// <summary>
    /// Checkbox state.
    /// 
    /// False = In none of the collections
    /// Null = In some of the collections
    /// True = In all the collections
    /// </summary>
    bool? IsSelected { get; set; }
}
