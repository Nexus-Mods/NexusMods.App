using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.VisualTree;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.UI;
using R3;

namespace NexusMods.App.UI.Pages.LoadoutPage.Dialogs.CollectionPublished;

public interface IDialogCollectionPublishedViewModel : IViewModelInterface
{
    string CollectionName { get; }
    CollectionStatus CollectionStatus { get; }
    Uri CollectionUrl { get; }
    ReactiveCommand<Unit> CommandCopyUrl { get; }
    bool FirstPublish { get;  }
}

public class DialogCollectionPublishedViewModel : AViewModel<IDialogCollectionPublishedViewModel>, IDialogCollectionPublishedViewModel
{
    public string CollectionName { get; }
    public CollectionStatus CollectionStatus { get; set; }
    public Uri CollectionUrl { get; }
    public ReactiveCommand<Unit> CommandCopyUrl { get; }
    public bool FirstPublish { get; }

    public DialogCollectionPublishedViewModel(string collectionName, CollectionStatus collectionStatus, Uri collectionUrl, bool firstPublish = false)
    {
        CollectionName = collectionName;
        CollectionUrl = collectionUrl;
        CollectionStatus = collectionStatus;
        FirstPublish = firstPublish;

        CommandCopyUrl = new ReactiveCommand<Unit>(async (_, cancellationToken) =>
        {
            await GetClipboard().SetTextAsync(CollectionUrl.AbsoluteUri);
        });
    }
    
    private IClipboard GetClipboard() {

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } window }) {
            return window.Clipboard!;
        }

        return null!;
    }
}
