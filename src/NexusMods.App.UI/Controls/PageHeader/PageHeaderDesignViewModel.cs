using NexusMods.Abstractions.UI;
using NexusMods.Icons;

namespace NexusMods.App.UI.Controls.PageHeader;

public class PageHeaderDesignViewModel : AViewModel<IPageHeaderViewModel>, IPageHeaderViewModel
{
    public string Title { get; set; } = "Page Title";
    public string Description { get; set; } = "Page description that can be really long";
    public IconValue Icon { get; set; } = IconValues.PictogramBox2;
}
