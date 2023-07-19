namespace NexusMods.App.UI.Controls.DataGrid;

/// <summary>
/// Defines a column that can be sorted and defines a way to compare two rows.
/// </summary>
/// <typeparam name="TRow"></typeparam>
public interface IComparableColumn<TRow>
{
    int Compare(TRow a, TRow b);
}
