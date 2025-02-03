using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using DynamicData;
using JetBrains.Annotations;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.UI.Extensions;
using NexusMods.App.UI.Controls;
using NexusMods.MnemonicDB.Abstractions;
using ObservableCollections;
using R3;

namespace NexusMods.App.UI.Pages.LibraryPage;

[Obsolete("Use CompositeItemModel instead")]
public interface ILibraryItemModel : ITreeDataGridItemModel<ILibraryItemModel, EntityId>;
