namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

/// <summary>
/// Defines a column that can be sorted and defines a way to compare two rows.
/// </summary>
/// <typeparam name="TRow"></typeparam>
public interface IComparableColumn<TRow>
{
    int Compare(TRow a, TRow b);
}
