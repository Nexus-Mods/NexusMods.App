using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.LibraryPage;

public class FakeParentLibraryItemModel : LibraryItemModel
{
    public required IObservable<int> NumInstalledObservable { get; init; }
    public required IObservable<int> NumLibraryItemsObservable { get; init; }

    private readonly IDisposable _modelActivationDisposable;
    public FakeParentLibraryItemModel()
    {
        _modelActivationDisposable = WhenModelActivated(this, static (model, disposables) =>
        {
            model.NumInstalledObservable
                .ToObservable()
                .CombineLatest(
                    source2: model.NumLibraryItemsObservable.ToObservable(),
                    source3: model.WhenAnyValue(static model => model.IsExpanded).ToObservable(),
                    source4: model.IsInstalledInLoadout,
                    static (a,b,c , _) => (a,b,c)
                )
                .ObserveOnUIThreadDispatcher()
                .Subscribe(model, static (tuple, model) =>
                {
                    var (numInstalled, numCount, isExpanded) = tuple;

                    if (numInstalled > 0)
                    {
                        if (numInstalled == numCount)
                        {
                            model.InstallText.Value = "Installed";
                        } else {
                            model.InstallText.Value = $"Installed {numInstalled}/{numCount}";
                        }
                    } else {
                        if (!isExpanded && numCount == 1)
                        {
                            model.InstallText.Value = "Install";
                        } else {
                            model.InstallText.Value = $"Install ({numCount})";
                        }
                    }
                })
                .AddTo(disposables);
        });
    }

    private bool _isDisposed;
    protected override void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                _modelActivationDisposable.Dispose();
            }

            _isDisposed = true;
        }

        base.Dispose(disposing);
    }
}
