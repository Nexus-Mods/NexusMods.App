using Avalonia.Media;
using JetBrains.Annotations;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Controls;

[UsedImplicitly]
[JsonName("NexusMods.App.UI.Controls.DummyPageParameter")]
public record DummyPageContext : IPageFactoryContext
{
    public Color Color { get; init; } = Color.FromRgb(GetRandomByte(), GetRandomByte(), GetRandomByte());

    private static byte GetRandomByte()
    {
        var i = Random.Shared.Next(byte.MinValue, byte.MaxValue);
        return (byte)i;
    }
}

[UsedImplicitly]
public class DummyPageFactory : APageFactory<DummyViewModel, DummyPageContext>
{
    public DummyPageFactory(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public static readonly PageFactoryId StaticId = PageFactoryId.From(Guid.Parse("71eeb62b-1d2a-45ec-9924-aa0a80a60478"));
    public override PageFactoryId Id => StaticId;

    public override DummyViewModel CreateViewModel(DummyPageContext context)
    {
        return new DummyViewModel(WorkspaceController)
        {
            Color = context.Color
        };
    }

#if DEBUG
    public override IEnumerable<PageDiscoveryDetails?> GetDiscoveryDetails()
    {
        yield return new PageDiscoveryDetails
        {
            // TODO: translations?
            SectionName = "Debug Utilities",
            ItemName = "Dummy Page",
            PageData = new PageData
            {
                Context = new DummyPageContext(),
                FactoryId = Id
            }
        };
    }
#endif
}
