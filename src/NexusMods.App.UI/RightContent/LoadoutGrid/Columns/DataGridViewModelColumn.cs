using Avalonia;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

public class DataGridViewModelColumn<TVm, TRow> : ADataGridViewModelColumn<TVm, TRow> where TVm : IColumnViewModel<TRow> {
    private readonly IServiceProvider _provider;
    private readonly IViewLocator _locator;

    public DataGridViewModelColumn(IServiceProvider provider,
        IViewLocator locator)
    {
        _provider = provider;
        _locator = locator;
    }

    protected override Control GenerateElement(DataGridCell cell, object dataItem)
    {
        var vm = _provider.GetService<TVm>();
        if (vm == null)
            return new TextBlock { Text = $"No column VM found for {typeof(TVm)}" };
        vm!.Row = (TRow)dataItem;

        var view = _locator.ResolveView(vm);
        if (view == null)
            return new TextBlock { Text = $"No column view found for VM {typeof(TVm)}" };

        view.ViewModel = vm;
        if (view is StyledElement styled)
            styled.DataContext = vm;

        return (Control)view;
    }
}
