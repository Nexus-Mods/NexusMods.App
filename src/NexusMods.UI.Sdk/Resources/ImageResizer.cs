using NexusMods.Sdk.Resources;
using SkiaSharp;

namespace NexusMods.UI.Sdk.Resources;

public sealed class ImageResizer<TResourceIdentifier> : ANestedResourceLoader<TResourceIdentifier, SKBitmap, SKBitmap>
    where TResourceIdentifier : notnull
{
    private readonly SKSizeI _newSize;
    private readonly bool _maintainAspectRatio;

    public ImageResizer(
        SKSizeI newSize,
        IResourceLoader<TResourceIdentifier, SKBitmap> innerLoader,
        bool maintainAspectRatio = true) : base(innerLoader)
    {
        _newSize = newSize;
        _maintainAspectRatio = maintainAspectRatio;
    }

    protected override ValueTask<Resource<SKBitmap>> ProcessResourceAsync(
        Resource<SKBitmap> resource,
        TResourceIdentifier resourceIdentifier,
        CancellationToken cancellationToken)
    {
        var original = resource.Data;

        var targetSize = _newSize;

        /*
         * If Width is larger, the Height is calculated. If Height is larger, the Width is calculated. This ensures the resizing
         * is based on the dominant dimension while maintaining the aspect ratio
        */
        if (_maintainAspectRatio)
        {
            var originalAspectRatio = (float)original.Width / original.Height;
            var targetAspectRatio = (float)_newSize.Width / _newSize.Height;
    
            if (originalAspectRatio > targetAspectRatio)
            {
                // Original is wider compared to target - width becomes the limiting factor
                targetSize = new SKSizeI(_newSize.Width, (int)(_newSize.Width / originalAspectRatio));
            }
            else
            {
                // Original is taller compared to target - height becomes the limiting factor
                targetSize = new SKSizeI((int)(_newSize.Height * originalAspectRatio), _newSize.Height);
            }
        }

        return ValueTask.FromResult(resource with
            {
                // previous Resize method was obsolete
                Data = original.Resize(targetSize, SKSamplingOptions.Default),
            }
        );
    }
}

public static partial class Extensions
{
    public static IResourceLoader<TResourceIdentifier, SKBitmap> Resize<TResourceIdentifier>(
        this IResourceLoader<TResourceIdentifier, SKBitmap> inner,
        SKSizeI newSize,
        bool maintainAspectRatio = true)
        where TResourceIdentifier : notnull
    {
        return inner.Then(
            state: (newSize, maintainAspectRatio),
            factory: static (state, inner) => new ImageResizer<TResourceIdentifier>(
                newSize: state.newSize,
                innerLoader: inner,
                maintainAspectRatio: state.maintainAspectRatio
            )
        );
    }
}
