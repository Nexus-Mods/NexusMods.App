using System.Linq.Expressions;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using ReactiveUI;

namespace NexusMods.App.UI;

public static class ReactiveUIExtensions
{

    /// <summary>
    /// Run the current observable on the UI thread.
    /// </summary>
    /// <param name="observable"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IObservable<T> OnUI<T>(this IObservable<T> observable)
    {
        return observable.ObserveOn(RxApp.MainThreadScheduler);
    }

    /// <summary>
    /// Run the current observable off the UI thread.
    /// </summary>
    /// <param name="observable"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IObservable<T> OffUI<T>(this IObservable<T> observable)
    {
        return observable.ObserveOn(TaskPoolScheduler.Default);
    }
    public static IDisposable BindToUI<TValue, TTarget, TTValue>(
        this IObservable<TValue> @this,
        TTarget? target,
        Expression<Func<TTarget, TTValue?>> property,
        object? conversionHint = null,
        IBindingTypeConverter? vmToViewConverterOverride = null)
        where TTarget : class =>
        @this.OnUI().BindTo(target, property, conversionHint, vmToViewConverterOverride);

}
