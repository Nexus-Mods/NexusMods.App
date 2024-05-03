namespace NexusMods.App.UI.Controls.DataGrid;

/// <summary>
/// A interface for view models that are used in a cell, each one is bound to a value of
/// a given type, this value should be Reactive as it will be updated when the value changes.
/// </summary>
public interface ICellViewModel<TValue> : IViewModelInterface
{
    public TValue Value { get; set; }
}
