using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace NexusMods.App.UI.Controls;

/// <summary>
/// Custom element factory to set classes on created elements, so
/// we can target them from styles.
/// </summary>
public class CustomElementFactory : TreeDataGridElementFactory
{
    protected override Control CreateElement(object? data)
    {
        if (data is CustomCell customCell)
        {
            var element = base.CreateElement(customCell.Inner);

            element.Classes.Add(customCell.Id);
            if (customCell.IsRoot)
            {
                element.Classes.Add("RootRow");
            }

            return element;
        }

        return base.CreateElement(data);
    }

    protected override string GetDataRecycleKey(object? data)
    {
        // TODO(erri120): I think we need to implement this, otherwise cells for one column get put in another column
        return base.GetDataRecycleKey(data);
    }

    protected override string GetElementRecycleKey(Control element)
    {
        // TODO(erri120): I think we need to implement this, otherwise cells for one column get put in another column
        return base.GetElementRecycleKey(element);
    }
}
