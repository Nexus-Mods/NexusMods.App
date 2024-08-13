using DynamicData.Binding;
using NexusMods.Abstractions.Library.Models;
using NexusMods.MnemonicDB.Abstractions;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Library;

public class NexusModsModPageLibraryNode : LibraryNode
{
    private R3.ReactiveProperty<LibraryNode?> PrimaryFile { get; } = new();

    private readonly IDisposable _disposable;
    public NexusModsModPageLibraryNode()
    {
        var d = Disposable.CreateBuilder();

        Children
            .ObserveCollectionChanges()
            .Subscribe(_ =>
            {
                // TODO: use same file as shown on nexus mods
                var primaryFile = Children.MaxBy<LibraryNode, string>(static node => node.Version, StringComparer.OrdinalIgnoreCase);
                PrimaryFile.Value = primaryFile;

            })
            .AddTo(ref d);

        PrimaryFile.Subscribe(this, static (primaryFile, node) =>
        {
            node.Version = primaryFile?.Version ?? DefaultVersion;
            node.Size = primaryFile?.Size ?? DefaultSize;
            node.DateAddedToLoadout = primaryFile?.DateAddedToLoadout ?? DefaultDateAddedToLoadout;
        }).AddTo(ref d);

        PrimaryFile
            .Where(static node => node is not null)
            .Select(static node => node!)
            .Select(static node => node.WhenAnyValue(static node => node.DateAddedToLoadout).ToObservable())
            .Switch()
            .Subscribe(this, static (date, node) => node.DateAddedToLoadout = date)
            .AddTo(ref d);

        _disposable = d.Build();
    }

    public override LibraryItem.ReadOnly GetLibraryItemToInstall(IConnection connection)
    {
        var primaryFile = PrimaryFile.Value;
        if (primaryFile is null) return base.GetLibraryItemToInstall(connection);
        return primaryFile.GetLibraryItemToInstall(connection);
    }

    private bool _isDisposed;
    protected override void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                Disposable.Dispose(_disposable, PrimaryFile);
            }

            _isDisposed = true;
        }

        base.Dispose(disposing);
    }
}
