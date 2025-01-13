using NexusMods.Abstractions.UI;
using NexusMods.Icons;

namespace NexusMods.App.UI.Controls.PageHeader;

public class PageHeaderViewModel : AViewModel<IPageHeaderViewModel>, IPageHeaderViewModel
{
    public string Title { get; set; }
    public string Description { get; set; }
    public IconValue Icon { get; set; }
    
    public PageHeaderViewModel(string title, string description, IconValue icon)
    {
        Title = title;
        Description = description;
        Icon = icon;
    }
    
}
