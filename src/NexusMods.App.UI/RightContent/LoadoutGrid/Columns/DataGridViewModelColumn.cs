using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

/// <summary>
/// A data grid column that uses a view model to generate the content through the
/// IViewLocator service.
/// </summary>
/// <typeparam name="TVm"></typeparam>
/// <typeparam name="TRow"></typeparam>
public class DataGridViewModelColumn<TVm, TRow> : ADataGridViewModelColumn<TVm, TRow> where TVm : IColumnViewModel<TRow> {
    private readonly IServiceProvider _provider;
    private readonly IViewLocator _locator;

    public DataGridViewModelColumn(IServiceProvider provider,
        IViewLocator locator)
    {
        _provider = provider;
        _locator = locator;
        CustomSortComparer = CreateComparer();
    }

    private IComparer CreateComparer()
    {
        var vm = _provider.GetRequiredService<TVm>();

        if (vm is not IComparableColumn<TRow> cVm) return Comparer<object?>.Create((a, b) => 0);
        return Comparer<object?>.Create((a, b) =>
        {
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;
            return cVm.Compare((TRow)a, (TRow)b);
        });
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

        cell.Content = (Control)view;

        return (Control)view;
    }
}
