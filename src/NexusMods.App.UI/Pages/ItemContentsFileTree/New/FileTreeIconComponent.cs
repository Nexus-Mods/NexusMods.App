using JetBrains.Annotations;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Controls.Trees.Common;
using R3;
namespace NexusMods.App.UI.Pages.ItemContentsFileTree.New;

/// <summary>
/// Component for <see cref="FileTreeNodeIconType"/>.
/// </summary>
[PublicAPI]
public sealed class FileTreeIconComponent : AValueComponent<FileTreeNodeIconType>, IItemModelComponent<FileTreeIconComponent>, IComparable<FileTreeIconComponent>
{
    /// <inheritdoc/>
    public int CompareTo(FileTreeIconComponent? other) => other is null ? 1 : 0;

    /// <inheritdoc/>
    public FileTreeIconComponent(
        FileTreeNodeIconType initialValue,
        IObservable<FileTreeNodeIconType> valueObservable,
        bool subscribeWhenCreated = false) : base(initialValue, valueObservable, subscribeWhenCreated) { }

    /// <inheritdoc/>
    public FileTreeIconComponent(
        FileTreeNodeIconType initialValue,
        Observable<FileTreeNodeIconType> valueObservable,
        bool subscribeWhenCreated = false) : base(initialValue, valueObservable, subscribeWhenCreated) { }

    /// <inheritdoc/>
    public FileTreeIconComponent(FileTreeNodeIconType value) : base(value) { }
}
