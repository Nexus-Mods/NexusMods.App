using NexusMods.Abstractions.UI;
using NexusMods.Icons;

namespace NexusMods.App.UI.Controls.PageHeader;

public interface IPageHeaderViewModel : IViewModelInterface
{
    public string Title { get; set; }
    public string Description { get; set; }
    public IconValue Icon { get; set; }
}
