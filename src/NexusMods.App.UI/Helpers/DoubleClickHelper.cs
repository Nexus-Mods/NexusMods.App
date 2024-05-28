using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using JetBrains.Annotations;

namespace NexusMods.App.UI.Helpers;

[PublicAPI]
public static class DoubleClickHelper
{
    // NOTE(erri120):
    // Windows uses 500ms (https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setdoubleclicktime?redirectedfrom=MSDN#parameters)
    // Avalonia default is 500ms (https://github.com/AvaloniaUI/Avalonia/blob/b904ded9ab32022b3ce037c8b63d21887fbd85a0/src/Avalonia.Base/Platform/DefaultPlatformSettings.cs#L35)
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Registers a double click handler and returns an observable that triggers whenever
    /// a double click occurs.
    /// </summary>
    public static IObservable<Unit> AddDoubleClickHandler(
        this Control control,
        RoutingStrategies routingStrategy = RoutingStrategies.Bubble,
        bool ignoreHandledEvents = false)
    {
        return AddDoubleClickHandler(control, InputElement.PointerPressedEvent, routingStrategy, ignoreHandledEvents);
    }

    /// <summary>
    /// Registers a double click handler and returns an observable that triggers whenever
    /// a double click occurs.
    /// </summary>
    public static IObservable<Unit> AddDoubleClickHandler<TRoutedEventArgs>(
        this Control control,
        RoutedEvent<TRoutedEventArgs> routedEvent,
        RoutingStrategies routingStrategy = RoutingStrategies.Bubble,
        bool ignoreHandledEvents = false
    ) where TRoutedEventArgs : RoutedEventArgs
    {
        return new DoubleClickSubject<TRoutedEventArgs>(control, routedEvent, routingStrategy, ignoreHandledEvents);
    }

    private class DoubleClickSubject : ADoubleClickSubject<PointerPressedEventArgs>
    {
        public DoubleClickSubject(
            Control control,
            RoutedEvent<PointerPressedEventArgs> routedEvent,
            RoutingStrategies routingStrategy,
            bool ignoreHandledEvents) : base(control, routedEvent, routingStrategy, ignoreHandledEvents
        ) { }

        protected override bool IsDoubleClick(PointerPressedEventArgs args)
        {
            return args.ClickCount == 2;
        }
    }

    private class DoubleClickSubject<TRoutedEventArgs> : ADoubleClickSubject<TRoutedEventArgs>
        where TRoutedEventArgs : RoutedEventArgs
    {
        private readonly TimeSpan _timeout;
        private DateTime _lastDateTime = DateTime.UnixEpoch;

        public DoubleClickSubject(
            Control control,
            RoutedEvent<TRoutedEventArgs> routedEvent,
            RoutingStrategies routingStrategy,
            bool ignoreHandledEvents) : base(control, routedEvent, routingStrategy, ignoreHandledEvents
        )
        {
            var doubleTapTime = TopLevel.GetTopLevel(control)?.PlatformSettings?.GetDoubleTapTime(PointerType.Mouse);
            _timeout = doubleTapTime ?? DefaultTimeout;
        }

        protected override bool IsDoubleClick(TRoutedEventArgs args)
        {
            var currentDateTime = DateTime.Now;
            var isDoubleClick = currentDateTime - _lastDateTime <= _timeout;
            _lastDateTime = currentDateTime;

            return isDoubleClick;
        }
    }


    private abstract class ADoubleClickSubject<TRoutedEventArgs> : SubjectBase<Unit>
        where TRoutedEventArgs : RoutedEventArgs
    {
        private bool _isDisposed;

        private readonly List<IObserver<Unit>> _observers = new(capacity: 1);
        private readonly CompositeDisposable _compositeDisposable = new();

        protected ADoubleClickSubject(
            Control control,
            RoutedEvent<TRoutedEventArgs> routedEvent,
            RoutingStrategies routingStrategy,
            bool ignoreHandledEvents)
        {
            control.AddDisposableHandler(
                routedEvent,
                EventHandler,
                routes: routingStrategy,
                handledEventsToo: !ignoreHandledEvents
            ).DisposeWith(_compositeDisposable);
        }

        private void EventHandler(object? sender, TRoutedEventArgs args)
        {
            if (!IsDoubleClick(args)) return;
            args.Handled = true;
            OnNext(Unit.Default);
        }

        protected abstract bool IsDoubleClick(TRoutedEventArgs args);

        public override void Dispose()
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            _compositeDisposable.Dispose();
            _isDisposed = true;
        }

        public override void OnCompleted()
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            foreach (var observer in _observers)
            {
                observer.OnCompleted();
            }
        }

        public override void OnError(Exception error)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            foreach (var observer in _observers)
            {
                observer.OnError(error);
            }
        }

        public override void OnNext(Unit value)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            foreach (var observer in _observers)
            {
                observer.OnNext(value);
            }
        }

        public override IDisposable Subscribe(IObserver<Unit> observer)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            _observers.Add(observer);

            var disposable = Disposable.Create((_observers, observer), tuple =>
            {
                // ReSharper disable once VariableHidesOuterVariable
                var (observers, observer) = tuple;
                observers.Remove(observer);
            });

            disposable.DisposeWith(_compositeDisposable);
            return disposable;
        }

        public override bool HasObservers => _observers.Count > 0;
        public override bool IsDisposed => _isDisposed;
    }
}
