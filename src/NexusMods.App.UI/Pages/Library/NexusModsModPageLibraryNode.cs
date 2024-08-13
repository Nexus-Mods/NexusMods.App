using DynamicData.Binding;

namespace NexusMods.App.UI.Pages.Library;

public class NexusModsModPageLibraryNode : LibraryNode
{
    private readonly IDisposable _disposable;

    public NexusModsModPageLibraryNode()
    {
        _disposable = Children
            .ObserveCollectionChanges()
            .Subscribe(_ =>
            {
                // TODO: use same file as shown on nexus mods
                var primaryFile = Children.MaxBy<LibraryNode, string>(static node => node.Version, StringComparer.OrdinalIgnoreCase);
                Version = primaryFile?.Version ?? DefaultVersion;
                Size = primaryFile?.Size ?? DefaultSize;
            });
    }

    private bool _isDisposed;
    protected override void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                _disposable.Dispose();
            }

            _isDisposed = true;
        }

        base.Dispose(disposing);
    }
}
