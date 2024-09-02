using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace NexusMods.App.UI.Controls;

/// <summary>
/// Custom element factory to set classes on created elements, so
/// we can target them from styles.
/// </summary>
public class CustomElementFactory : TreeDataGridElementFactory
{
    // NOTE(erri120): Used in styles, don't change!
    private const string RootRowClass = "RootRow";
    
    protected override Control CreateElement(object? data)
    {
        var element = base.CreateElement(data);

        if (data is not ICustomCell customCell) return element;
        
        element.Classes.Add(customCell.Id);
        if (customCell.IsRoot)
        {
            element.Classes.Add(RootRowClass);
        }

        return element;
    }
    
    protected override string GetDataRecycleKey(object? data)
    {
        // NOTE(Al12rs): Cell recycling breaks Id and RootRowClass styling, since these are not reapplied when the cell is reused.
        // To fix this, we restrict the recycling to only reuse cells with matching Id and RootRowClass.
        // This is done by appending the Id and RootRowClass to the base key.
        
        if (data is ICustomCell customCell)
        {
            // NOTE(Al12rs): the keys generated here should match the ones in GetElementRecycleKey, ensure order and format is the same
            var key = customCell.IsRoot
                ? $"{base.GetDataRecycleKey(data)}|{customCell.Id}|{RootRowClass}"
                : $"{base.GetDataRecycleKey(data)}|{customCell.Id}";
            return string.Intern(key);
        }

        return base.GetDataRecycleKey(data);
    }

    protected override string GetElementRecycleKey(Control element)
    {
        // NOTE(Al12rs): Cell recycling breaks Id and RootRowClass styling, since these are not reapplied when the cell is reused.
        // To fix this, we restrict the recycling to only reuse cells with matching Id and RootRowClass.
        // This is done by appending the Id and RootRowClass to the base key.
        
        var sb = new StringBuilder(value: base.GetElementRecycleKey(element));
        
        // NOTE(Al12rs): Order here should match the insertion order in CreateElement, first the id, then the RootRowClass
        foreach (var className in element.Classes)
        {
            if (className is null) continue;
            if (className.StartsWith(':')) continue;
            sb.Append($"|{className}");
        }

        var key = string.Intern(sb.ToString());
        return key;
    }
}
