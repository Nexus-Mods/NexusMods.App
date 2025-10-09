using System.Diagnostics;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NexusMods.UI.Sdk;
using ReactiveUI;

namespace NexusMods.App.UI;

public static class ReactiveUiExtensions
{
    // NOTE(erri120): this field is set in Startup.cs
#pragma warning disable CA2211
    public static ILogger DefaultLogger = NullLogger.Instance;
#pragma warning restore CA2211

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
    /// <returns></returns>x
    public static IObservable<T> OffUi<T>(this IObservable<T> observable)
    {
        return observable.ObserveOn(RxApp.TaskpoolScheduler);
    }

    [Obsolete("This should not be used anymore. See UI Coding Conventions for more details.")]
    public static IDisposable BindToUi<TValue, TTarget, TTValue>(
        this IObservable<TValue> @this,
        TTarget? target,
        Expression<Func<TTarget, TTValue?>> property,
        object? conversionHint = null,
        IBindingTypeConverter? vmToViewConverterOverride = null)
        where TTarget : class =>
        @this.OnUI().BindTo(target, property, conversionHint, vmToViewConverterOverride);

    /// <summary>
    /// Binds the observable stream to a property on the View Model.
    /// </summary>
    public static IDisposable BindToVM<TValue, TTarget>(
        this IObservable<TValue> @this,
        TTarget target,
        Expression<Func<TTarget, TValue?>> property)
        where TTarget : class, IViewModel
    {
        Debug.Assert(Dispatcher.UIThread.CheckAccess());
        return @this.BindTo(target, property, conversionHint: null, vmToViewConverterOverride: null);
    }

    /// <summary>
    /// Binds the observable stream to a property on the View.
    /// </summary>
    public static IDisposable BindToView<TValue, TTarget>(
        this IObservable<TValue> @this,
        TTarget target,
        Expression<Func<TTarget, TValue?>> property)
        where TTarget : class, IViewFor
    {
        Debug.Assert(Dispatcher.UIThread.CheckAccess());
        return @this.BindTo(target, property, conversionHint: null, vmToViewConverterOverride: null);
    }

    /// <summary>
    /// Subscribes to the specified source, re-routing synchronous exceptions during invocation of the
    /// <see cref="ObservableExtensions.Subscribe{T}(System.IObservable{T})"/> method to
    /// <see cref="DefaultLogger"/>. This method should be used instead of <see cref="ObservableExtensions.Subscribe{T}(System.IObservable{T})"/>.
    /// See https://github.com/Nexus-Mods/NexusMods.App/issues/328 for more information.
    /// </summary>
    /// <param name="source">Observable sequence to subscribe to.</param>
    /// <param name="onNext">Action to invoke for each element in the observable sequence.</param>
    /// <typeparam name="TValue">The type of the elements in the source sequence.</typeparam>
    /// <returns><see cref="IDisposable"/> object used to unsubscribe from the observable sequence.</returns>
    /// <see cref="SubscribeWithErrorLogging{TValue}(System.IObservable{TValue})"/>
    /// <seealso cref="SubscribeWithErrorLogging{TValue}(System.IObservable{TValue},Microsoft.Extensions.Logging.ILogger)"/>
    /// <seealso cref="SubscribeWithErrorLogging{TValue}(System.IObservable{TValue},Microsoft.Extensions.Logging.ILogger,Action{TValue})"/>
    public static IDisposable SubscribeWithErrorLogging<TValue>(this IObservable<TValue> source, Action<TValue> onNext)
    {
        return source.SubscribeSafe(new ExceptionObserverWithOnNext<TValue>(DefaultLogger, onNext));
    }

    /// <summary>
    /// Subscribes to the specified source, re-routing synchronous exceptions during invocation of the
    /// <see cref="ObservableExtensions.Subscribe{T}(System.IObservable{T})"/> method to
    /// <see cref="DefaultLogger"/>. This method should be used instead of <see cref="ObservableExtensions.Subscribe{T}(System.IObservable{T})"/>.
    /// See https://github.com/Nexus-Mods/NexusMods.App/issues/328 for more information.
    /// </summary>
    /// <param name="source">Observable sequence to subscribe to.</param>
    /// <typeparam name="TValue">The type of the elements in the source sequence.</typeparam>
    /// <returns><see cref="IDisposable"/> object used to unsubscribe from the observable sequence.</returns>
    /// <see cref="SubscribeWithErrorLogging{TValue}(System.IObservable{TValue},System.Action{TValue})"/>
    /// <seealso cref="SubscribeWithErrorLogging{TValue}(System.IObservable{TValue},Microsoft.Extensions.Logging.ILogger)"/>
    /// <seealso cref="SubscribeWithErrorLogging{TValue}(System.IObservable{TValue},Microsoft.Extensions.Logging.ILogger,Action{TValue})"/>
    public static IDisposable SubscribeWithErrorLogging<TValue>(this IObservable<TValue> source)
    {
        return source.SubscribeSafe(new ExceptionObserver<TValue>(DefaultLogger));
    }

    /// <summary>
    /// Subscribes to the specified source, re-routing synchronous exceptions during invocation of the
    /// <see cref="ObservableExtensions.Subscribe{T}(System.IObservable{T})"/> method to the
    /// provided <see cref="ILogger"/>. This method should be used instead of <see cref="ObservableExtensions.Subscribe{T}(System.IObservable{T})"/>.
    /// See https://github.com/Nexus-Mods/NexusMods.App/issues/328 for more information.
    /// </summary>
    /// <param name="source">Observable sequence to subscribe to.</param>
    /// <param name="logger">Logger used for logging synchronous exceptions. <see cref="DefaultLogger"/> will be used if the value is <c>null</c>.</param>
    /// <typeparam name="TValue">The type of the elements in the source sequence.</typeparam>
    /// <returns><see cref="IDisposable"/> object used to unsubscribe from the observable sequence.</returns>
    /// <see cref="SubscribeWithErrorLogging{TValue}(System.IObservable{TValue})"/>
    /// <see cref="SubscribeWithErrorLogging{TValue}(System.IObservable{TValue},System.Action{TValue})"/>
    /// <seealso cref="SubscribeWithErrorLogging{TValue}(System.IObservable{TValue},Microsoft.Extensions.Logging.ILogger,Action{TValue})"/>
    public static IDisposable SubscribeWithErrorLogging<TValue>(
        this IObservable<TValue> source,
        ILogger? logger)
    {
        return source.SubscribeSafe(new ExceptionObserver<TValue>(logger ?? DefaultLogger));
    }

    /// <summary>
    /// Subscribes to the specified source, re-routing synchronous exceptions during invocation of the
    /// <see cref="ObservableExtensions.Subscribe{T}(System.IObservable{T})"/> method to the
    /// provided <see cref="ILogger"/>. This method should be used instead of <see cref="ObservableExtensions.Subscribe{T}(System.IObservable{T})"/>.
    /// See https://github.com/Nexus-Mods/NexusMods.App/issues/328 for more information.
    /// </summary>
    /// <param name="source">Observable sequence to subscribe to.</param>
    /// <param name="logger">Logger used for logging synchronous exceptions. <see cref="DefaultLogger"/> will be used if the value is <c>null</c>.</param>
    /// <param name="onNext">Action to invoke for each element in the observable sequence.</param>
    /// <typeparam name="TValue">The type of the elements in the source sequence.</typeparam>
    /// <returns><see cref="IDisposable"/> object used to unsubscribe from the observable sequence.</returns>
    /// <see cref="SubscribeWithErrorLogging{TValue}(System.IObservable{TValue})"/>
    /// <see cref="SubscribeWithErrorLogging{TValue}(System.IObservable{TValue},System.Action{TValue})"/>
    /// <seealso cref="SubscribeWithErrorLogging{TValue}(System.IObservable{TValue},Microsoft.Extensions.Logging.ILogger)"/>
    public static IDisposable SubscribeWithErrorLogging<TValue>(
        this IObservable<TValue> source,
        ILogger? logger,
        Action<TValue> onNext)
    {
        return source.SubscribeSafe(new ExceptionObserverWithOnNext<TValue>(logger ?? DefaultLogger, onNext));
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
        protected override void OnNextCore(T value)
        {
            try
            {
                _onNext(value);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception from Observable in onNext");
            }
        }

        protected override void OnCompletedCore() { }
    }
}
