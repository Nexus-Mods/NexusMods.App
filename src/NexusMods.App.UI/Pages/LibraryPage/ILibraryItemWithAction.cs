using System.Diagnostics.CodeAnalysis;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.App.UI.Controls;
using NexusMods.Paths;
using OneOf;
using R3;

namespace NexusMods.App.UI.Pages.LibraryPage;

public interface ILibraryItemWithAction : ILibraryItemModel, IComparable<ILibraryItemWithAction>, IColumnDefinition<ILibraryItemModel, ILibraryItemWithAction>
{
    int IComparable<ILibraryItemWithAction>.CompareTo(ILibraryItemWithAction? other)
    {
        return (this, other) switch
        {
            (ILibraryItemWithUpdateAction, ILibraryItemWithInstallAction) => -1, // updates before install
            (ILibraryItemWithUpdateAction, ILibraryItemWithDownloadAction) => -1, // updates before download
            
            (ILibraryItemWithInstallAction, ILibraryItemWithDownloadAction) => 1, // install after download
            (ILibraryItemWithDownloadAction, ILibraryItemWithInstallAction) => -1, // download before install

            // should sort by job status, completed comes after running and none is at the top
            (ILibraryItemWithDownloadAction a, ILibraryItemWithDownloadAction b) => ((int)a.DownloadState.Value).CompareTo((int)b.DownloadState.Value),

            (ILibraryItemWithInstallAction a, ILibraryItemWithInstallAction b) => a.IsInstalled.Value.CompareTo(b.IsInstalled.Value),

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

public class DownloadableItem : OneOfBase<CollectionDownloadNexusMods.ReadOnly, CollectionDownloadExternal.ReadOnly>
{
    public DownloadableItem(OneOf<CollectionDownloadNexusMods.ReadOnly, CollectionDownloadExternal.ReadOnly> input) : base(input) { }
}

public interface ILibraryItemWithDownloadAction : ILibraryItemWithAction
{
    DownloadableItem DownloadableItem { get; }

    ReactiveCommand<Unit, DownloadableItem> DownloadItemCommand { get; }

    BindableReactiveProperty<JobStatus> DownloadState { get; }

    BindableReactiveProperty<string> DownloadButtonText { get; }

    public static ReactiveCommand<Unit, DownloadableItem> CreateCommand<TModel>(TModel model)
        where TModel : ILibraryItemModel, ILibraryItemWithDownloadAction
    {
        var canDownload = model.DownloadState.Select(static state => state < JobStatus.Running);
        return canDownload.ToReactiveCommand<Unit, DownloadableItem>(_ => model.DownloadableItem, initialCanExecute: false);
    }

    public static string GetButtonText(JobStatus status)
    {
        return status switch
        {
            < JobStatus.Running => "Download",
            JobStatus.Running => "Downloading",
            JobStatus.Paused => "Paused",
            JobStatus.Completed => "Downloaded",
            JobStatus.Cancelled => "Cancelled",
            JobStatus.Failed => "Failed",
        };
    }
}

/// <summary>
/// Extends the 'Install' action with the update capabilities; allowing install and update
/// on some menus and only 'Install' on others.
/// </summary>
public interface ILibraryItemWithUpdateAction : ILibraryItemWithInstallAction
{
    ReactiveCommand<Unit, ILibraryItemModel> UpdateItemCommand { get; }

    BindableReactiveProperty<bool> UpdateAvailable { get; }

    BindableReactiveProperty<string> UpdateButtonText { get; }

    public static ReactiveCommand<Unit, ILibraryItemModel> CreateCommand<TModel>(TModel model)
        where TModel : ILibraryItemModel, ILibraryItemWithUpdateAction
    {
        var canUpdate = model.UpdateAvailable.Select(static hasUpdate => hasUpdate);
        return canUpdate.ToReactiveCommand<Unit, ILibraryItemModel>(_ => model, initialCanExecute: false);
    }

    public static string GetButtonText(int numUpdatable, int numTotal, bool alwaysShowWithCount)
    {
        // 'Update ({0})'
        if (alwaysShowWithCount)
            return string.Format(Resources.Language.LibraryItemButtonUpdate_CounterInBracket, numUpdatable);
        
        // 'Update'
        if (numUpdatable == 1)
            return Resources.Language.LibraryItemButtonUpdate_Single;
        
        // 'Update All'
        if (numUpdatable == numTotal)
            return Resources.Language.LibraryItemButtonUpdate_All;

        // 'Update ({0})'
        return string.Format(Resources.Language.LibraryItemButtonUpdate_CounterInBracket, numUpdatable);
    }
}
