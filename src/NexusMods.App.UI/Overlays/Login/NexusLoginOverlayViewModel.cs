using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData;
using NexusMods.Abstractions.Activities;
using NexusMods.Networking.NexusWebApi.NMA;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Overlays.Login;

public class NexusLoginOverlayViewModel : AViewModel<INexusLoginOverlayViewModel>, INexusLoginOverlayViewModel, IDisposable
{
    private readonly CompositeDisposable _compositeDisposable;

    public NexusLoginOverlayViewModel(IActivityMonitor activityMonitor, IOverlayController overlayController)
    {
        _compositeDisposable = new CompositeDisposable();

        var currentJob = activityMonitor.Activities
            .AsObservableChangeSet(x => x.Id)
            .QueryWhenChanged(q => q.Items.FirstOrDefault(activity => activity.Group == OAuth.Group))
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
                    IsActive = false;
                    return;
                }

                IsActive = true;
                overlayController.SetOverlayContent(new SetOverlayItem(this));
            })
            .DisposeWith(_compositeDisposable);

        currentJob
            .WhereNotNull()
            .Select(job => ReactiveCommand.Create(job.Cancel))
            .BindTo(this, vm => vm.Cancel)
            .DisposeWith(_compositeDisposable);
    }

    [Reactive]
    public ICommand Cancel { get; set; } = Initializers.ICommand;

    [Reactive]
    public Uri Uri { get; set; } = new("https://www.nexusmods.com");

    [Reactive]
    public bool IsActive { get; set; }

    public void Dispose()
    {
        _compositeDisposable.Dispose();
    }
}
