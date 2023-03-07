using System.Linq.Expressions;
using System.Reactive.Linq;
using ReactiveUI;

namespace NexusMods.App.UI;

public static class ReactiveUIExtensions
{

    public static IObservable<T> OnUI<T>(this IObservable<T> observable)
    {
        return observable.ObserveOn(RxApp.MainThreadScheduler);
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
