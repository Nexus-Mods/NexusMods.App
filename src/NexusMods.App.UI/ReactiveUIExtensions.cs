using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace NexusMods.App.UI;

public static class ReactiveUiExtensions
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
    public static IObservable<T> OffUi<T>(this IObservable<T> observable)
    {
        return observable.ObserveOn(TaskPoolScheduler.Default);
    }

    public static IDisposable BindToUi<TValue, TTarget, TTValue>(
        this IObservable<TValue> @this,
        TTarget? target,
        Expression<Func<TTarget, TTValue?>> property,
        object? conversionHint = null,
        IBindingTypeConverter? vmToViewConverterOverride = null)
        where TTarget : class =>
        @this.OnUI().BindTo(target, property, conversionHint, vmToViewConverterOverride);

    /// <summary>
    /// Subscribes to the specified source, re-routing synchronous exceptions during invocation of the
    /// <see cref="ObservableExtensions.Subscribe{T}(System.IObservable{T})"/> method to the
    /// provided <see cref="ILogger"/>. This method should be used instead of <see cref="ObservableExtensions.Subscribe{T}(System.IObservable{T})"/>.
    /// See https://github.com/Nexus-Mods/NexusMods.App/issues/328 for more information.
    /// </summary>
    /// <param name="source">Observable sequence to subscribe to.</param>
    /// <param name="logger">Logger used for logging synchronous exceptions.</param>
    /// <typeparam name="TValue">The type of the elements in the source sequence.</typeparam>
    /// <returns><see cref="IDisposable"/> object used to unsubscribe from the observable sequence.</returns>
    /// <seealso cref="SubscribeWithErrorLogging{TValue}(System.IObservable{TValue},Microsoft.Extensions.Logging.ILogger,Action{TValue})"/>
    public static IDisposable SubscribeWithErrorLogging<TValue>(
        this IObservable<TValue> source,
        ILogger logger)
    {
        return source.SubscribeSafe(new ExceptionObserver<TValue>(logger));
    }

    /// <summary>
    /// Subscribes to the specified source, re-routing synchronous exceptions during invocation of the
    /// <see cref="ObservableExtensions.Subscribe{T}(System.IObservable{T})"/> method to the
    /// provided <see cref="ILogger"/>. This method should be used instead of <see cref="ObservableExtensions.Subscribe{T}(System.IObservable{T})"/>.
    /// See https://github.com/Nexus-Mods/NexusMods.App/issues/328 for more information.
    /// </summary>
    /// <param name="source">Observable sequence to subscribe to.</param>
    /// <param name="logger">Logger used for logging synchronous exceptions.</param>
    /// <param name="onNext">Action to invoke for each element in the observable sequence.</param>
    /// <typeparam name="TValue">The type of the elements in the source sequence.</typeparam>
    /// <returns><see cref="IDisposable"/> object used to unsubscribe from the observable sequence.</returns>
    /// <seealso cref="SubscribeWithErrorLogging{TValue}(System.IObservable{TValue},Microsoft.Extensions.Logging.ILogger)"/>
    public static IDisposable SubscribeWithErrorLogging<TValue>(
        this IObservable<TValue> source,
        ILogger logger,
        Action<TValue> onNext)
    {
        return source.SubscribeSafe(new ExceptionObserverWithOnNext<TValue>(logger, onNext));
    }

    private sealed class ExceptionObserver<T> : ObserverBase<T>
    {
        private readonly ILogger _logger;

        public ExceptionObserver(ILogger logger)
        {
            _logger = logger;
        }

        protected override void OnErrorCore(Exception error) => _logger.LogError(error, "Exception from Observable");
        protected override void OnNextCore(T value) { }
        protected override void OnCompletedCore() { }
    }

    private sealed class ExceptionObserverWithOnNext<T> : ObserverBase<T>
    {
        private readonly ILogger _logger;
        private readonly Action<T> _onNext;

        public ExceptionObserverWithOnNext(ILogger logger, Action<T> onNext)
        {
            _logger = logger;
            _onNext = onNext;
        }

        protected override void OnErrorCore(Exception error) => _logger.LogError(error, "Exception from Observable");
        protected override void OnNextCore(T value) => _onNext(value);
        protected override void OnCompletedCore() { }
    }
}
