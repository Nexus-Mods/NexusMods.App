using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData;
using DynamicData.Binding;

namespace NexusMods.App.UI.Extensions;

public static class ObservableExtensions
{
    public static IConnectableObservable<T> PublishWithFunc<T>(this IObservable<T> source, Func<T> initialValueFunc)
    {
        return source.Multicast(new PublishWithFuncSubject<T>(func: initialValueFunc));
    }

    public static IObservable<T> ReturnFactory<T>(Func<T> factory)
    {
        return Observable.Create<T>(observer =>
        {
            observer.OnNext(factory());
            return Disposable.Empty;
        });
    }

    private class PublishWithFuncSubject<T> : SubjectBase<T>
    {
        private readonly Subject<T> _subject = new();
        private readonly Func<T> _func;

        public PublishWithFuncSubject(Func<T> func)
        {
            _func = func;
        }

        public override void Dispose() => _subject.Dispose();
        public override void OnCompleted() => _subject.OnCompleted();
        public override void OnError(Exception error) => _subject.OnError(error);
        public override void OnNext(T value) => _subject.OnNext(value);

        public override IDisposable Subscribe(IObserver<T> observer)
        {
            observer.OnNext(_func());
            return _subject.Subscribe(observer);
        }

        public override bool HasObservers => _subject.HasObservers;
        public override bool IsDisposed => _subject.IsDisposed;
    }
}
