using Avalonia.Controls;
using Avalonia.Controls.Utils;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

public abstract class ADataGridViewModelColumn<TVmType, TRowType> : DataGridColumn where TVmType : IColumnViewModel<TRowType>
{

    public ADataGridViewModelColumn()
    {
        IsReadOnly = true;
    }

    protected override Control GenerateEditingElement(DataGridCell cell, object dataItem,
        out ICellEditBinding binding)
    {
        throw new NotImplementedException();
    }

    /*
    protected override Control GenerateElement(DataGridCell cell, object dataItem)
    {
        var vm = _provider.GetService<TVmType>();
        if (vm == null)
            return new TextBlock { Text = $"No column VM found for {typeof(TVmType)}" };
        vm!.Row = (TRowType)dataItem;

        var view = _locator.ResolveView(vm);
        if (view == null)
            return new TextBlock { Text = $"No column view found for VM {typeof(TVmType)}" };

        view.ViewModel = vm;
        return (Control)view;
    }
    */

    protected override object PrepareCellForEdit(Control editingElement,
        RoutedEventArgs editingEventArgs)
    {
        throw new NotImplementedException();
    }
}
