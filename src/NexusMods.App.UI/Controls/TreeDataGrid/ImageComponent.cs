using Avalonia.Media.Imaging;
using BitFaster.Caching;
using JetBrains.Annotations;
using NexusMods.Abstractions.Resources;
using R3;

namespace NexusMods.App.UI.Controls;

/// <summary>
/// Component for <see cref="Bitmap"/>.
/// </summary>
[PublicAPI]
public sealed class ImageComponent : AValueComponent<Bitmap>, IItemModelComponent<ImageComponent>, IComparable<ImageComponent>
{
    /// <inheritdoc/>
    public int CompareTo(ImageComponent? other) => other is null ? 1 : 0;

    /// <inheritdoc/>
    public ImageComponent(
        Bitmap initialValue,
        IObservable<Bitmap> valueObservable,
        bool subscribeWhenCreated = false) : base(initialValue, valueObservable, subscribeWhenCreated) { }

    /// <inheritdoc/>
    public ImageComponent(
        Bitmap initialValue,
        Observable<Bitmap> valueObservable,
        bool subscribeWhenCreated = false) : base(initialValue, valueObservable, subscribeWhenCreated) { }

    /// <inheritdoc/>
    public ImageComponent(Bitmap value) : base(value) { }

    public static ImageComponent FromPipeline<TId>(
        IResourceLoader<TId, Bitmap> pipeline,
        TId id,
        Bitmap initialValue)
        where TId : notnull
    {
        var observable = Observable
            .Return(id)
            .ObserveOnThreadPool()
            .SelectAwait(async (_, cancellationToken) => await pipeline.LoadResourceAsync(id, cancellationToken), configureAwait: false)
            .Select(static resource => resource.Data);

        return new ImageComponent(
            initialValue: initialValue,
            valueObservable: observable
        );
    }
}
