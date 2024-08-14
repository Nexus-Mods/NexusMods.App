using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace NexusMods.App.UI.Controls;

public class CustomElementFactory : TreeDataGridElementFactory
{
    protected override Control CreateElement(object? data)
    {
        if (data is CustomTextCell customCell)
        {
            var element = base.CreateElement(customCell.Inner);
            element.Classes.Add(customCell.Id);
            return element;
        }

        return base.CreateElement(data);
    }
}
