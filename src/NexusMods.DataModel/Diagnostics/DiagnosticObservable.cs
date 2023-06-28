using System.Reactive.Subjects;

namespace NexusMods.DataModel.Diagnostics;

internal class DiagnosticObservable : IDiagnosticObservable
{
    private readonly Subject<Diagnostic> _subject = new();

    public IDisposable Subscribe(IObserver<Diagnostic> observer)
    {
        return _subject.Subscribe(observer);
    }

    public void Emit(Diagnostic diagnostic)
    {
        _subject.OnNext(diagnostic);
    }
}
