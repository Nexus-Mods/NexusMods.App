using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using NexusMods.DataModel.Interprocess.Jobs;
using NexusMods.DataModel.Interprocess.Messages;
using NexusMods.Networking.NexusWebApi.NMA.Types;
using NexusMods.Networking.NexusWebApi.Types;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Overlays;

public class NexusLoginOverlayViewModel : AViewModel<INexusLoginOverlayViewModel>, INexusLoginOverlayViewModel, IDisposable
{
    private readonly CompositeDisposable _compositeDisposable;

    public NexusLoginOverlayViewModel(IInterprocessJobManager jobManager)
    {
        _compositeDisposable = new CompositeDisposable();

        var currentJob = jobManager.Jobs
            .QueryWhenChanged(q =>
                q.Items.FirstOrDefault(j => j.Payload is NexusLoginJob));

        currentJob.WhereNotNull()
            .Select(job => ((NexusLoginJob)job.Payload).Uri)
            .BindToUi(this, vm => vm.Uri)
            .DisposeWith(_compositeDisposable);

        currentJob.Select(job => job != null)
            .BindToUi(this, vm => vm.IsActive)
            .DisposeWith(_compositeDisposable);

        currentJob
            .WhereNotNull()
            .Select(job => ReactiveCommand.Create(() => jobManager.EndJob(job.JobId)))
            .BindToUi(this, vm => vm.Cancel)
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
