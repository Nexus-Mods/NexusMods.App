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
    ReactiveCommand<Unit, Unit> InstallItemCommand { get; }

    BindableReactiveProperty<bool> IsInstalled { get; }

    BindableReactiveProperty<string> InstallButtonText { get; }
}

public interface ILibraryItemWithDownloadAction : ILibraryItemWithAction
{
    ReactiveCommand<Unit, Unit> DownloadItemCommand { get; }
}
