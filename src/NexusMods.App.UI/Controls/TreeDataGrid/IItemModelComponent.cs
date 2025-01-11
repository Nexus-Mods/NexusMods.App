using System.ComponentModel;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData.Kernel;
using JetBrains.Annotations;
using R3;

namespace NexusMods.App.UI.Controls;

[PublicAPI]
public interface IItemModelComponent;

[PublicAPI]
public interface IItemModelComponent<TSelf> : IItemModelComponent
    where TSelf : class, IItemModelComponent<TSelf>, IComparable<TSelf>
{
    static virtual int Compare<TKey>(CompositeItemModel<TKey>? a, CompositeItemModel<TKey>? b)
        where TKey : notnull
    {
        return (a, b) switch
        {
            ({ } itemA, { } itemB) => TSelf.Compare(itemA.TryGet<TSelf>(out var valueA) ? valueA : null, itemB.TryGet<TSelf>(out var valueB) ? valueB : null),

            // b precedes a
            (not null, null) => 1,

            // a precedes b
            (null, not null) => -1,

            // a and b are in the same position
            (null, null) => 0,
        };
    }

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

    static virtual IColumn<CompositeItemModel<TKey>> CreateColumn<TKey>(Optional<ListSortDirection> sortDirection = default)
        where TKey : notnull
    {
        return new CustomTemplateColumn<CompositeItemModel<TKey>>(
            header: TSelf.GetColumnHeader(),
            cellTemplateResourceKey: TSelf.GetColumnTemplateResourceKey(),
            options: new TemplateColumnOptions<CompositeItemModel<TKey>>
            {
                CanUserSortColumn = true,
                CanUserResizeColumn = true,
                CompareAscending = static (a, b) => TSelf.Compare(a, b),
                CompareDescending = static (a, b) => TSelf.Compare(b, a),
            }
        )
        {
            Id = TSelf.GetColumnId(),
            SortDirection = sortDirection.HasValue ? sortDirection.Value : null,
        };
    }

    static abstract string GetColumnHeader();
    static abstract string GetColumnTemplateResourceKey();
    static virtual string GetColumnId() => TSelf.GetColumnTemplateResourceKey();
    static virtual ComponentKey GetKey() => ComponentKey.From(TSelf.GetColumnTemplateResourceKey());
}
