namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

public interface IColumnViewModel<TRow> : IViewModelInterface
{
    public TRow Row { get; set; }
}
