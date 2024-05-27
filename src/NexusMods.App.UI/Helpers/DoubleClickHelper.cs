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
    // NOTE(erri120): Windows uses 500ms (https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setdoubleclicktime?redirectedfrom=MSDN#parameters)
    // This can be fine-tuned later.
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Registers a double click handler and returns an observable that triggers whenever
    /// a double click occurs.
    /// </summary>
    public static IObservable<Unit> AddDoubleClickHandler(this Control control)
    {
        return AddDoubleClickHandler(control, InputElement.PointerPressedEvent);
    }

    /// <summary>
    /// Registers a double click handler and returns an observable that triggers whenever
    /// a double click occurs.
    /// </summary>
    public static IObservable<Unit> AddDoubleClickHandler<TRoutedEventArgs>(this Control control, RoutedEvent<TRoutedEventArgs> routedEvent)
        where TRoutedEventArgs : RoutedEventArgs
    {
        return new DoubleClickSubject<TRoutedEventArgs>(control, routedEvent);
    }

    private class DoubleClickSubject<TRoutedEventArgs> : SubjectBase<Unit>
        where TRoutedEventArgs : RoutedEventArgs
    {
        private bool _isDisposed;

        private readonly List<IObserver<Unit>> _observers = new(capacity: 1);
        private readonly IDisposable _handlerDisposable;

        public DoubleClickSubject(Control control, RoutedEvent<TRoutedEventArgs> routedEvent)
        {
            _handlerDisposable = control.AddDisposableHandler(
                routedEvent,
                EventHandler,
                routes: RoutingStrategies.Bubble,
                handledEventsToo: true
            );
        }

        private DateTime _lastDateTime = DateTime.UnixEpoch;
        private void EventHandler(object? sender, TRoutedEventArgs args)
        {
            var currentDateTime = DateTime.Now;
            var isDoubleClick = currentDateTime - _lastDateTime <= DefaultTimeout;
            _lastDateTime = currentDateTime;

            if (!isDoubleClick) return;
            OnNext(Unit.Default);
            args.Handled = true;
        }

        public override void Dispose()
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            _handlerDisposable.Dispose();
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

            return Disposable.Create((_observers, observer), tuple =>
            {
                // ReSharper disable once VariableHidesOuterVariable
                var (observers, observer) = tuple;
                observers.Remove(observer);
            });
        }

        public override bool HasObservers => _observers.Count > 0;
        public override bool IsDisposed => _isDisposed;
    }
}
