using System.Diagnostics.CodeAnalysis;
using NexusMods.App.UI.Controls;
using R3;

namespace NexusMods.App.UI.Pages.LibraryPage;

public interface ILibraryItemWithAction : ILibraryItemModel, IComparable<ILibraryItemWithAction>, IColumnDefinition<ILibraryItemModel, ILibraryItemWithAction>
{
    int IComparable<ILibraryItemWithAction>.CompareTo(ILibraryItemWithAction? other)
    {
        return (this, other) switch
        {
            (ILibraryItemWithInstallAction, ILibraryItemWithDownloadAction) => -1,
            (ILibraryItemWithDownloadAction, ILibraryItemWithInstallAction) => 1,
            _ => 0,
        };
    }

    public const string ColumnTemplateResourceKey = "LibraryItemActionColumn";
    static string IColumnDefinition<ILibraryItemModel, ILibraryItemWithAction>.GetColumnHeader() => "Action";
    static string IColumnDefinition<ILibraryItemModel, ILibraryItemWithAction>.GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
}

public interface ILibraryItemWithInstallAction : ILibraryItemWithAction
{
    ReactiveCommand<Unit, ILibraryItemModel> InstallItemCommand { get; }

    BindableReactiveProperty<bool> IsInstalled { get; }

    BindableReactiveProperty<string> InstallButtonText { get; }

    public static ReactiveCommand<Unit, ILibraryItemModel> CreateCommand<TModel>(TModel model)
        where TModel : ILibraryItemModel, ILibraryItemWithInstallAction
    {
        var canInstall = model.IsInstalled.Select(static isInstalled => !isInstalled);
        return canInstall.ToReactiveCommand<Unit, ILibraryItemModel>(_ => model, initialCanExecute: false);
    }

    public static string GetButtonText(bool isInstalled) => isInstalled ? "Installed" : "Install";

    [SuppressMessage("ReSharper", "RedundantIfElseBlock")]
    [SuppressMessage("ReSharper", "ConvertIfStatementToReturnStatement")]
    public static string GetButtonText(int numInstalled, int numTotal, bool isExpanded)
    {
        if (numInstalled > 0)
        {
            if (numInstalled == numTotal)
            {
                return "Installed";
            } else {
                return $"Installed {numInstalled}/{numTotal}";
            }
        }
        else
        {
            if (!isExpanded && numTotal == 1)
            {
                return "Install";
            } else {
                return $"Install ({numTotal})";
            }
        }
    }
}

public interface ILibraryItemWithDownloadAction : ILibraryItemWithAction
{
    ReactiveCommand<Unit, Unit> DownloadItemCommand { get; }

    BindableReactiveProperty<DownloadState> DownloadState { get; }

    BindableReactiveProperty<string> DownloadButtonText { get; }

    public static ReactiveCommand<Unit, Unit> CreateCommand<TModel>(TModel model)
        where TModel : ILibraryItemModel, ILibraryItemWithDownloadAction
    {
        var canDownload = model.DownloadState.Select(static state => state == LibraryPage.DownloadState.NotDownloaded);
        return canDownload.ToReactiveCommand<Unit, Unit>(static x => x, initialCanExecute: false);
    }

    public static string GetButtonText(DownloadState state)
    {
        return state switch
        {
            LibraryPage.DownloadState.NotDownloaded => "Download",
            LibraryPage.DownloadState.Downloading => "Downloading",
            LibraryPage.DownloadState.Downloaded => "Downloaded",
        };
    }
}

public enum DownloadState
{
    NotDownloaded,
    Downloading,
    Downloaded,
}
