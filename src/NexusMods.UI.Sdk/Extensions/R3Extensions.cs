using System.ComponentModel;
using JetBrains.Annotations;
using R3;

namespace NexusMods.UI.Sdk;

/// <summary>
/// Extension methods for <see cref="R3"/>.
/// </summary>
[PublicAPI]
public static class R3Extensions
{
    /// <summary>
    /// One-way bind using R3.
    /// </summary>
    public static IDisposable OneWayR3Bind<TView, TViewModel, TValue>(
        this TView view,
        Func<TView, BindableReactiveProperty<TViewModel?>> viewModelSelector, // NOTE(erri120): this is fucking stupid
        Func<TViewModel, IReadOnlyBindableReactiveProperty<TValue>> vmPropertySelector,
        Action<TView, TValue> updater)
        where TView : IR3View<TViewModel>
        where TViewModel : class, INotifyPropertyChanged
    {
        return view
            .ObserveViewModelProperty(viewModelSelector, vmPropertySelector)
            .ObserveOnUIThreadDispatcher()
            .Subscribe((view, updater), static (itemCount, state) => state.updater(state.view, itemCount));
    }

    /// <summary>
    /// Two-way bind using R3.
    /// </summary>
    public static IDisposable TwoWayR3Bind<TView, TViewModel, TViewModelValue, TViewProperty>(
        this TView view,
        Func<TView, BindableReactiveProperty<TViewModel?>> viewModelSelector, // NOTE(erri120): this is fucking stupid
        Func<TViewModel, IBindableReactiveProperty<TViewModelValue>> vmPropertySelector,
        Func<TView, TViewProperty> viewPropertySelector,
        Func<TViewProperty, TViewModelValue> viewToVMConverter,
        Action<TView, TViewModelValue> vmToView)
        where TView : IR3View<TViewModel>
        where TViewModel : class, INotifyPropertyChanged
    {
        var serialDisposable = new SerialDisposable();

        var disposable = viewModelSelector(view).Subscribe((serialDisposable, view, viewPropertySelector, vmPropertySelector, viewToVMConverter, vmToView), static (vm, state) =>
        {
            var (serialDisposable, view, viewPropertySelector, vmPropertySelector, viewToVMConverter, vmToView) = state;

            if (vm is null)
            {
                serialDisposable.Disposable = null;
                return;
            }

            var disposable1 = view.ObservePropertyChanged(viewPropertySelector).Subscribe((vmPropertySelector, vm, viewToVMConverter), static (viewProperty, state) =>
            {
                var (vmPropertySelector, vm, viewToVMConverter) = state;
                vmPropertySelector(vm).Value = viewToVMConverter(viewProperty);
            });

            var disposable2 = vmPropertySelector(vm).AsObservable().Subscribe((vmToView, view), static (vmProperty, state) =>
            {
                var (vmToView, view) = state;
                vmToView(view, vmProperty);
            });

            serialDisposable.Disposable = Disposable.Combine(disposable1, disposable2);
        });

        return Disposable.Combine(serialDisposable, disposable);
    }

    /// <summary>
    /// Two-way bind using R3.
    /// </summary>
    public static IDisposable TwoWayR3Bind<TView, TViewModel, TValue>(
        this TView view,
        Func<TView, BindableReactiveProperty<TViewModel?>> viewModelSelector, // NOTE(erri120): this is fucking stupid
        Func<TViewModel, IBindableReactiveProperty<TValue>> vmPropertySelector,
        Func<TView, TValue> viewPropertySelector,
        Action<TView, TValue> vmToView)
        where TView : IR3View<TViewModel>
        where TViewModel : class, INotifyPropertyChanged
    {
        return TwoWayR3Bind(view, viewModelSelector, vmPropertySelector, viewPropertySelector, viewToVMConverter: static x => x, vmToView);
    }

    /// <summary>
    /// Returns an observable stream of a property on the View Model from the View.
    /// </summary>
    public static Observable<TValue> ObserveViewModelProperty<TView, TViewModel, TValue>(
        this TView view,
        Func<TView, BindableReactiveProperty<TViewModel?>> viewModelSelector, // NOTE(erri120): this is fucking stupid
        Func<TViewModel, IReadOnlyBindableReactiveProperty<TValue>> vmPropertySelector)
        where TView : IR3View<TViewModel>
        where TViewModel : class, INotifyPropertyChanged
    {
        return viewModelSelector(view)
            .WhereNotNull()
            .Select(vmPropertySelector)
            .SelectMany(reactiveProperty => reactiveProperty.AsObservable());
    }
}
