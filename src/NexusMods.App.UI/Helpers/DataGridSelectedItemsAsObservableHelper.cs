using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Avalonia.Controls;
using DynamicData;

namespace NexusMods.App.UI.Helpers;

/// <summary>
/// A helper class that provides an observable collection of selected items for a DataGrid using DynamicData and Observables.
/// Also see <see cref="DataGridSelectedItemsAsObservableHelperExtensions"/> before using.
/// </summary>
/// <typeparam name="T">The type of the items in the DataGrid.</typeparam>
/// <example>
/// Usage example:
/// <code>
/// public YourViewModel()
/// {
///     this.WhenActivated(disposables =&gt;
///     {
///         var helper = new DataGridSelectedItemsAsObservableHelper&lt;YourItemType&gt;(MyDataGrid);
///                      .DisposeWith(disposables);
///
///         // Use helper.SelectedItems
///
///         // That said, it is recommended you use DataGridSelectedItemsAsObservableHelperExtensions
///     });
/// }
/// </code>
/// </example>
public class DataGridSelectedItemsAsObservableHelper<T> : IDisposable where T : notnull
{
    private readonly DataGrid _dataGrid;
    private readonly SourceCache<T, T> _selectedItemsCache;

    /// <summary>
    /// Contains the currently selected items as an observable.
    /// </summary>
    public IObservable<IChangeSet<T, T>> SelectedItems { get; init; }

    public DataGridSelectedItemsAsObservableHelper(DataGrid dataGrid)
    {
        _dataGrid = dataGrid;
        _selectedItemsCache = new SourceCache<T, T>(x => x);
        SelectedItems = _selectedItemsCache.Connect();
        _dataGrid.SelectionChanged += DataGridOnSelectionChanged;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _dataGrid.SelectionChanged -= DataGridOnSelectionChanged;
        _selectedItemsCache.Dispose();
    }

    private void DataGridOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // Note(sewer) It's important we use Edit here because the event should be atomic
        // If we do an `WhenValueChanged` without an Edit, the ReadOnlyObservableCollection
        // derived from the SourceCache will have a brief period of time where it shows
        // an item which should just have been removed.
        _selectedItemsCache.Edit(updater =>
        {
            foreach (var removedItem in e.RemovedItems)
            {
                Debug.Assert(removedItem is T, "Removed item is not of the correct type");
                Debug.Assert(removedItem != null, "Removed item is null");
                updater.RemoveKey((T)removedItem);
            }

            foreach (var addedItem in e.AddedItems)
            {    
                Debug.Assert(addedItem is T, "Added item is not of the correct type");
                Debug.Assert(addedItem != null, "Added item is null");
                updater.AddOrUpdate((T)addedItem);
            }
        });
    }
}

/// <summary>
/// Extension methods for synchronizing the selected items of a DataGrid with an existing property on a view model using the DataGridSelectedItemsAsObservableHelper.
/// </summary>
public static class DataGridSelectedItemsAsObservableHelperExtensions
{
    /// <summary>
    /// Synchronizes the selected items of the current DataGrid with an existing
    /// property on a view model using the DataGridSelectedItemsAsObservableHelper.
    ///
    /// This gets us a DynamicData observable for the selected items of a DataGrid.
    /// </summary>
    /// <typeparam name="T">The type of the items in the DataGrid.</typeparam>
    /// <typeparam name="TVM">Type of ViewModel used.</typeparam>
    /// <param name="dataGrid">The current DataGrid instance.</param>
    /// <param name="viewModel">The view model instance.</param>
    /// <param name="selectedItemsProperty">The property on the view model to synchronize with.</param>
    /// <example>
    /// Usage example:
    /// <code>
    /// public class YourView : ReactiveUserControl&lt;YourViewModel&gt;
    /// {
    ///     public YourView()
    ///     {
    ///         this.WhenActivated(d =>
    ///         {
    ///             // Synchronize the selected items with the SelectedItems property on the view model using the helper
    ///             MyDataGrid.SelectedItemsToProperty(ViewModel, vm => vm.SelectedItems)
    ///                       .DisposeWith(d);
    ///         });
    ///     }
    /// }
    /// 
    /// public class YourViewModel : ReactiveObject, IActivatableViewModel
    /// {
    ///     public IObservable&lt;IChangeSet&lt;YourItemType&gt;&gt; SelectedItems { get; private set; }
    /// 
    ///     public YourViewModel()
    ///     {
    ///         // Initialize other properties
    ///     }
    /// }
    /// </code>
    /// </example>
    public static IDisposable SelectedItemsToProperty<T, TVM>(this DataGrid dataGrid, TVM viewModel, Expression<Func<TVM, IObservable<IChangeSet<T, T>>>> selectedItemsProperty) where T : notnull
    {
        var propertyInfo = ((MemberExpression)selectedItemsProperty.Body).Member as PropertyInfo;
        if (propertyInfo == null)
            throw new ArgumentException("The selectedItemsProperty must be a property.");

        var selectedItemsHelper = new DataGridSelectedItemsAsObservableHelper<T>(dataGrid);
        propertyInfo.SetValue(viewModel, selectedItemsHelper.SelectedItems);
        return selectedItemsHelper;
    }
}
