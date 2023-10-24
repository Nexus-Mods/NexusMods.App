using Avalonia.Media;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.DataModel.JsonConverters;

namespace NexusMods.App.UI.Controls;

public record DummyPageParameter(Color Color) : IPageFactoryParameter;

[JsonName("NexusMods.App.UI.Controls.DummyPageData")]
public record DummyPageData : APageData
{
    public required DummyPageParameter Parameter { get; init; }
}

public class DummyPageFactory : IPageFactory<DummyPage, DummyPageParameter>
{
    public PageFactoryId Id => PageFactoryId.From(Guid.Parse("71eeb62b-1d2a-45ec-9924-aa0a80a60478"));

    public DummyPage Create(DummyPageParameter parameter)
    {
        return new DummyPage
        {
            ViewModel = new DummyViewModel
            {
                Color = parameter.Color
            },
            PageData = new DummyPageData
            {
                FactoryId = Id,
                Parameter = parameter
            }
        };
    }
}
