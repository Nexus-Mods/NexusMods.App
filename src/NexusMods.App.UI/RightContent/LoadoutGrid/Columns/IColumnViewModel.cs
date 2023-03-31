namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

/// <summary>
/// Interface for view models that are used in a column, each one is bound to a row,
/// this row should be Reactive as it will be updated when the row changes.
/// </summary>
/// <typeparam name="TRow"></typeparam>
public interface IColumnViewModel<TRow> : IViewModelInterface
{
    public TRow Row { get; set; }
}
