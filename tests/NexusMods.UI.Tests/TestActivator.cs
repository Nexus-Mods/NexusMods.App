using System.Reactive.Subjects;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.UI.Tests;

public class TestActivator : IActivationForViewFetcher
{
    private readonly Dictionary<IActivatableView,Subject<bool>> _views;

    public TestActivator()
    {
        // Activators
        _views = new Dictionary<IActivatableView, Subject<bool>>();
    }

    public int GetAffinityForView(Type view)
    {
        // During testing, we should always be the one to activate the view.
        return 100;
    }

    public IObservable<bool> GetActivationForView(IActivatableView view)
    {
        lock (_views)
        {
            if (_views.TryGetValue(view, out var activator))
            {
                return activator;
            }

            activator = new Subject<bool>();
            _views.Add(view, activator);

            return activator;
        }
    }

    public Subject<bool> GetActivation(IActivatableView control)
    {
        lock (_views)
        {
            return _views[control];
        }
    }
}
