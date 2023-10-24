using Avalonia.Media;
using JetBrains.Annotations;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.DataModel.JsonConverters;

namespace NexusMods.App.UI.Controls;

[UsedImplicitly]
[JsonName("NexusMods.App.UI.Controls.DummyPageParameter")]
public record DummyPageParameter : IPageFactoryParameter
{
    public Color Color { get; init; } = Color.FromRgb(GetRandomByte(), GetRandomByte(), GetRandomByte());

    private static byte GetRandomByte()
    {
        var i = Random.Shared.Next(byte.MinValue, byte.MaxValue);
        return (byte)i;
    }
}

[UsedImplicitly]
public class DummyPageFactory : IPageFactory<DummyViewModel, DummyPageParameter>
{
    public static readonly PageFactoryId Id = PageFactoryId.From(Guid.Parse("71eeb62b-1d2a-45ec-9924-aa0a80a60478"));
    PageFactoryId IPageFactory.Id => Id;

    public DummyViewModel CreateViewModel(DummyPageParameter parameter)
    {
        return new DummyViewModel
        {
            Color = parameter.Color
        };
    }
}
