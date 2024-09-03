using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
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

        switch (data)
        {
            // Add RootRowClass to root rows
            case IIndentedRow { Indent: 0 }:
                element.Classes.Add(RootRowClass);
                break;
            // Add Id to custom cells
            case ICustomCell customCell:
                element.Classes.Add(customCell.Id);
                break;
        }

        return element;
    }
    
    protected override string GetDataRecycleKey(object? data)
    {
        // NOTE(Al12rs): Cell recycling breaks Id and RootRowClass styling, since these are not reapplied when the cell is reused.
        // To fix this, we restrict the recycling to only reuse cells with matching Id and RootRowClass.
        // This is done by appending the Id and RootRowClass to the base key.
        
        switch (data)
        {
            case IIndentedRow:
                var rowKey = $"{base.GetDataRecycleKey(data)}|{RootRowClass}";
                return string.Intern(rowKey);
            case ICustomCell customCell:
            {
                // NOTE(Al12rs): the keys generated here should match the ones in GetElementRecycleKey, ensure order and format is the same
                var cellKey = $"{base.GetDataRecycleKey(data)}|{customCell.Id}";
                return string.Intern(cellKey);
            }
            default:
                return base.GetDataRecycleKey(data);
        }
    }

    protected override string GetElementRecycleKey(Control element)
    {
        // NOTE(Al12rs): Cell recycling breaks Id and RootRowClass styling, since these are not reapplied when the cell is reused.
        // To fix this, we restrict the recycling to only reuse cells with matching Id and RootRowClass.
        // This is done by appending the Id and RootRowClass to the base key.
        
        var sb = new StringBuilder(value: base.GetElementRecycleKey(element));
        
        if (element is not TreeDataGridCell or TreeDataGridRow)
            return string.Intern(sb.ToString());
        
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
