using System.Collections.Concurrent;
using System.Xml;
using Avalonia.Media;
using Avalonia.Svg.Skia;
using NexusMods.Abstractions.Resources;

namespace NexusMods.Media;

public sealed class SvgLoader<TResourceIdentifier, TInnerType> : ANestedResourceLoader<TResourceIdentifier, IImage, byte[]>
    where TResourceIdentifier : notnull
    where TInnerType : IImage
{
    public delegate IResourceLoader<TResourceIdentifier, TInnerType> Factory(IResourceLoader<TResourceIdentifier, byte[]> inner);

    private readonly IResourceLoader<TResourceIdentifier, TInnerType> _next;
    private readonly ConcurrentDictionary<TResourceIdentifier, Resource<byte[]>> _resources = new();

    public SvgLoader(
        Factory factory,
        IResourceLoader<TResourceIdentifier, byte[]> innerLoader
    ) : base(innerLoader)
    {
        var loader = ResourceLoader.Create<TResourceIdentifier, byte[], ConcurrentDictionary<TResourceIdentifier, Resource<byte[]>>>(_resources, static (resources, resourceIdentifier, _) =>
        {
            var resource = resources[resourceIdentifier];
            return ValueTask.FromResult(resource);
        });

        _next = factory.Invoke(loader);
    }

    protected override async ValueTask<Resource<IImage>> ProcessResourceAsync(
        Resource<byte[]> resource,
        TResourceIdentifier resourceIdentifier,
        CancellationToken cancellationToken)
    {
        if (IsSvg(resource.Data))
        {
            using var stream = new MemoryStream(resource.Data, writable: false);
            var source = SvgSource.LoadFromStream(stream);
            var svgImage = new SvgImage
            {
                Source = source,
            };

            return resource.WithData<IImage>(svgImage);
        }

        var didAdd = _resources.TryAdd(resourceIdentifier, resource);

        try
        {
            var res = await _next.LoadResourceAsync(resourceIdentifier, cancellationToken);
            return res.Cast<TInnerType, IImage>();
        }
        finally
        {
            if (didAdd) _resources.TryRemove(resourceIdentifier, out _);
        }
    }

    private static bool IsSvg(byte[] data)
    {
        using var stream = new MemoryStream(data, writable: false);
        try
        {
            var firstByte = stream.ReadByte();
            if (firstByte != ('<' & 0xFF)) return false;

            stream.Seek(0, SeekOrigin.Begin);
            using var xmlReader = XmlReader.Create(stream);
            return xmlReader.MoveToContent() == XmlNodeType.Element && "svg".Equals(xmlReader.Name, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}

public static partial class Extensions
{
    public static IResourceLoader<TResourceIdentifier, IImage> EnableSvgSupport<TResourceIdentifier, TInnerType>(
        this IResourceLoader<TResourceIdentifier, byte[]> inner,
        SvgLoader<TResourceIdentifier, TInnerType>.Factory factory)
        where TResourceIdentifier : notnull
        where TInnerType : IImage
    {
        return inner.Then(
            state: factory,
            factory: static (factory, inner) => new SvgLoader<TResourceIdentifier,TInnerType>(
                factory: factory,
                innerLoader: inner
            )
        );
    }
}
