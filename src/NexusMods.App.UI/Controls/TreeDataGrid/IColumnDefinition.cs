using Avalonia.Controls.Models.TreeDataGrid;
using JetBrains.Annotations;

namespace NexusMods.App.UI.Controls;

/// <summary>
/// Static interface for column definitions
/// </summary>
[PublicAPI]
public interface IColumnDefinition<TModel, TSelf>
    where TModel : class
    where TSelf : IColumnDefinition<TModel, TSelf>, IComparable<TSelf>
{
    static virtual int Compare(TModel? a, TModel? b)
    {
        return (a, b) switch
        {
            (TSelf itemA, TSelf itemB) => itemA.CompareTo(itemB),

            // b precedes a
            (TSelf, _) => 1,

            // a precedes b
            (_, TSelf) => -1,

            // a and b are in the same position
            (_, _) => 0,
        };
    }

    static abstract string GetColumnHeader();
    static abstract string GetColumnTemplateResourceKey();
    static virtual string GetColumnId() => TSelf.GetColumnTemplateResourceKey();

    static virtual IColumn<TModel> CreateColumn()
    {
        return new CustomTemplateColumn<TModel>(
            header: TSelf.GetColumnHeader(),
            cellTemplateResourceKey: TSelf.GetColumnTemplateResourceKey(),
            options: new TemplateColumnOptions<TModel>
            {
                CanUserSortColumn = true,
                CanUserResizeColumn = true,
                CompareAscending = static (a, b) => TSelf.Compare(a, b),
                CompareDescending = static (a, b) => TSelf.Compare(b, a),
            }
        )
        {
            Id = TSelf.GetColumnId(),
        };
    }
}

/// <summary>
/// Column helper.
/// </summary>
[PublicAPI]
public static class ColumnCreator
{
    /// <summary>
    /// Creates a column from a static interface definition.
    /// </summary>
    public static IColumn<TModel> CreateColumn<TModel, TColumnInterface>()
        where TModel : class
        where TColumnInterface : IColumnDefinition<TModel, TColumnInterface>, IComparable<TColumnInterface>
    {
        return TColumnInterface.CreateColumn();
    }
}

