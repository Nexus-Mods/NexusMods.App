using NexusMods.App.UI.Controls;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.App.UI.Pages.LibraryPage;

[Obsolete("Use CompositeItemModel instead")]
public interface ILibraryItemModel : ITreeDataGridItemModel<ILibraryItemModel, EntityId>;
