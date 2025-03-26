using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData.Kernel;
using JetBrains.Annotations;

namespace NexusMods.App.UI.Controls;

/// <summary>
/// Represents a component in a composite item model.
/// </summary>
[PublicAPI]
public interface IItemModelComponent;

/// <summary>
/// Represents a component in a composite item model.
/// </summary>
[PublicAPI]
public interface IItemModelComponent<in TSelf> : IItemModelComponent
    where TSelf : class, IItemModelComponent<TSelf>, IComparable<TSelf>
{
    static virtual int Compare(TSelf? a, TSelf? b)
    {
        return (a, b) switch
        {
            ({ } valueA, { } valueB) => valueA.CompareTo(valueB),

            // b precedes a
            (not null, null) => 1,

            // a precedes b
            (null, not null) => -1,

            // a and b are in the same position
            (null, null) => 0,
        };
    }
}

public static partial class ColumnCreator
{
    public static IColumn<CompositeItemModel<TKey>> Create<TKey, TColumn>(
        Optional<string> columnHeader = default,
        Optional<ListSortDirection> sortDirection = default,
        Optional<GridLength> width = default,
        bool canUserSortColumn = true,
        bool canUserResizeColumn = true)
        where TKey : notnull
        where TColumn : class, ICompositeColumnDefinition<TColumn>
    {
        return TColumn.CreateColumn<TKey>(
            columnHeader: columnHeader,
            sortDirection: sortDirection,
            width: width,
            canUserSortColumn: canUserSortColumn,
            canUserResizeColumn: canUserResizeColumn
        );
    }
}
