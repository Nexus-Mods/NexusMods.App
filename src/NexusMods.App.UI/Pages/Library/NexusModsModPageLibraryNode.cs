using ObservableCollections;
using R3;

namespace NexusMods.App.UI.Pages.Library;

public class NexusModsModPageLibraryNode : LibraryNode
{
    private readonly IDisposable _disposable;

    public NexusModsModPageLibraryNode()
    {
        _disposable = Children
            .ObserveCountChanged()
            .Subscribe(this, static (_, node) =>
            {
                // TODO: use same file as shown on nexus mods
                var primaryFile = Enumerable.MaxBy<LibraryNode, string>(node.Children, static node => node.Version, StringComparer.OrdinalIgnoreCase);
                node.Version = primaryFile?.Version ?? DefaultVersion;
                node.Size = primaryFile?.Size ?? DefaultSize;
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
