using Avalonia.Media;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.DataModel.JsonConverters;

namespace NexusMods.App.UI.Controls;

[JsonName("NexusMods.App.UI.Controls.DummyPageParameter")]
public record DummyPageParameter : IPageFactoryParameter
{
    public required Color Color { get; init; }
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
            PageData = new PageData
            {
                FactoryId = Id,
                Parameter = parameter
            }
        };
    }
}
