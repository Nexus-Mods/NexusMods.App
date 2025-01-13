using System.ComponentModel;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData.Kernel;
using JetBrains.Annotations;

namespace NexusMods.App.UI.Controls;

[PublicAPI]
public interface IItemModelComponent;

[PublicAPI]
public interface IItemModelComponent<TSelf> : IItemModelComponent
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
    public static IColumn<CompositeItemModel<TKey>> Create<TKey, TColumn>(Optional<ListSortDirection> sortDirection = default)
        where TKey : notnull
        where TColumn : class, ICompositeColumnDefinition<TColumn>
    {
        return TColumn.CreateColumn<TKey>(sortDirection);
    }
}
