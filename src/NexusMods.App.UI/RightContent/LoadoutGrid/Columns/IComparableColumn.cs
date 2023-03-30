namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

public interface IComparableColumn<TRow>
{
    int Compare(TRow a, TRow b);
}
