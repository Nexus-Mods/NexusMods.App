using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData;
using NexusMods.Abstractions.Activities;
using NexusMods.Abstractions.NexusWebApi;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Overlays.Login;

public class NexusLoginOverlayViewModel : AOverlayViewModel<INexusLoginOverlayViewModel>, INexusLoginOverlayViewModel, IDisposable
{
    private readonly CompositeDisposable _compositeDisposable;

    public NexusLoginOverlayViewModel(IActivityMonitor activityMonitor, IOverlayController overlayController)
    {
        _compositeDisposable = new CompositeDisposable();

        var currentJob = activityMonitor.Activities
            .AsObservableChangeSet(x => x.Id)
            .Transform(x => x)
            .QueryWhenChanged(q => q.Items.FirstOrDefault(activity => activity.Group.Value == Constants.OAuthActivityGroupName))
            .OnUI();

        currentJob.WhereNotNull()
            .Select(activity => activity.Payload as Uri)
            .BindTo(this, vm => vm.Uri)
            .DisposeWith(_compositeDisposable);

        currentJob.Select(job => job != null)
            .Subscribe(b =>
            {
                if (!b)
                {
                    Close();
                    return;
                }
                
                overlayController.Enqueue(this);
            })
            .DisposeWith(_compositeDisposable);

        currentJob
            .WhereNotNull()
            .Select(job => (IActivitySource)job)
            .Select(job => ReactiveCommand.Create(job.Dispose))
            .BindTo(this, vm => vm.Cancel)
            .DisposeWith(_compositeDisposable);
    }

    [Reactive]
    public ICommand Cancel { get; set; } = Initializers.ICommand;

    [Reactive]
    public Uri Uri { get; set; } = new("https://www.nexusmods.com");
    
    public void Dispose()
    {
        _compositeDisposable.Dispose();
    }
}
