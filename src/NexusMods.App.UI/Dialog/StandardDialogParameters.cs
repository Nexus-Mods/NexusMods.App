using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.UI.Sdk.Icons;

namespace NexusMods.App.UI.Dialog;

public struct StandardDialogParameters
{
    public string Text { get; set; }
    public string Heading { get; set; }
    public IconValue? Icon { get; set; }
    public IMarkdownRendererViewModel? Markdown { get; set; }
    public string InputLabel { get; set; }
    public string InputWatermark { get; set; }
    public string InputText { get; set; }
    public string BottomText { get; set; }
}
