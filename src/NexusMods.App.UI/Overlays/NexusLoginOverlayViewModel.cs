using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using NexusMods.DataModel.Interprocess.Jobs;
using NexusMods.DataModel.Interprocess.Messages;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Overlays;

public class NexusLoginOverlayViewModel : AViewModel<INexusLoginOverlayViewModel>, INexusLoginOverlayViewModel, IDisposable
{
    private readonly CompositeDisposable _compositeDisposable;

    public NexusLoginOverlayViewModel(IInterprocessJobManager jobManager)
    {
        _compositeDisposable = new CompositeDisposable();

        var uris = jobManager.Jobs.ToCollection()
            .Select(g =>
                g.FirstOrDefault(j => j.JobType == JobType.NexusLogin)
                    ?.PayloadAsUri);

        uris.WhereNotNull()
            .BindToUi(this, vm => vm.Uri)
            .DisposeWith(_compositeDisposable);

        uris.Select(uri => uri != null)
            .BindToUi(this, vm => vm.IsActive)
            .DisposeWith(_compositeDisposable);

    }

    [Reactive]
    public ICommand Cancel { get; set; } = Initializers.ICommand;

    [Reactive]
    public Uri Uri { get; set; } = new("https://www.nexusmods.com");

    public bool IsActive { get; set; }

    public void Dispose()
    {
        _compositeDisposable.Dispose();
    }
}
