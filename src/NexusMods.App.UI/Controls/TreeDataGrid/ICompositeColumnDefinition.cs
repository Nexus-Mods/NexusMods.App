using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.App.UI.Extensions;

namespace NexusMods.App.UI.Controls;

[PublicAPI]
public interface ICompositeColumnDefinition<TSelf>
    where TSelf : class, ICompositeColumnDefinition<TSelf>
{
    static virtual int Compare<TKey>(Optional<CompositeItemModel<TKey>> a, Optional<CompositeItemModel<TKey>> b)
        where TKey : notnull
    {
        return a.Compare(b, static (a, b) => TSelf.Compare(a, b));
    }

    static abstract int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull;

    static virtual IColumn<CompositeItemModel<TKey>> CreateColumn<TKey>(
        Optional<string> columnHeader = default,
        Optional<ListSortDirection> sortDirection = default,
        Optional<GridLength> width = default,
        bool canUserSortColumn = true,
        bool canUserResizeColumn = true)
        where TKey : notnull
    {
        return new CustomTemplateColumn<CompositeItemModel<TKey>>(
            header: columnHeader.HasValue ? columnHeader.Value : TSelf.GetColumnHeader(),
            cellTemplateResourceKey: TSelf.GetColumnTemplateResourceKey(),
            options: new TemplateColumnOptions<CompositeItemModel<TKey>>
            {
                CanUserSortColumn = canUserSortColumn,
                CanUserResizeColumn = canUserResizeColumn,
                CompareAscending = static (a, b) => TSelf.Compare(Optional<CompositeItemModel<TKey>>.Create(a), b),
                CompareDescending = static (a, b) => TSelf.Compare(Optional<CompositeItemModel<TKey>>.Create(b), a),
            },
            width: width.ValueOrDefault()
        )
        {
            Id = TSelf.GetColumnId(),
            SortDirection = sortDirection.HasValue ? sortDirection.Value : null,
        };
    }

    static abstract string GetColumnHeader();
    static abstract string GetColumnTemplateResourceKey();
    static virtual string GetColumnId() => TSelf.GetColumnTemplateResourceKey();
}
