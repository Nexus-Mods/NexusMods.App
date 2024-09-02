using System.Reactive.Subjects;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Data;

namespace NexusMods.App.UI.Controls;

public class CustomTextCell<T> : TextCell<T>, ICustomCell
{
    public required string Id { get; init; }
    public required bool IsRoot { get; init; }

    public CustomTextCell(T? value) : base(value) { }

    public CustomTextCell(ISubject<BindingValue<T>> binding, bool isReadOnly, ITextCellOptions? options = null) : base(binding, isReadOnly, options) { }
}
