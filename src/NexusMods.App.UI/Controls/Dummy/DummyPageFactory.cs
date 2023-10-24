using Avalonia.Media;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.DataModel.JsonConverters;

namespace NexusMods.App.UI.Controls;

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

public class DummyPageFactory : IPageFactory<DummyPage, DummyPageParameter>
{
    public static readonly PageFactoryId Id = PageFactoryId.From(Guid.Parse("71eeb62b-1d2a-45ec-9924-aa0a80a60478"));
    PageFactoryId IPageFactory.Id => Id;

    public DummyPage Create(DummyPageParameter parameter, PageData pageData)
    {
        return new DummyPage
        {
            ViewModel = new DummyViewModel
            {
                Color = parameter.Color
            },
            PageData = pageData
        };
    }
}
