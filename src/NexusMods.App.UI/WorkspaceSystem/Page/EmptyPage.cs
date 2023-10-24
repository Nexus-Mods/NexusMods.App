using NexusMods.DataModel.JsonConverters;

namespace NexusMods.App.UI.WorkspaceSystem;

public class EmptyPage : IPage
{
    public IViewModel? ViewModel { get; set; }

    public APageData PageData { get; set; } = new EmptyPageData
    {
        FactoryId = PageFactoryId.From(Guid.Parse("38d7c953-c237-49bb-8da4-319fa4989564"))
    };
}

public record EmptyPageParameter : IPageFactoryParameter;

[JsonName("NexusMods.App.UI.WorkspaceSystem.EmptyPageData")]
public record EmptyPageData : APageData;

public class EmptyPageFactory : IPageFactory<EmptyPage, EmptyPageParameter>
{
    public PageFactoryId Id => PageFactoryId.From(Guid.Parse("38d7c953-c237-49bb-8da4-319fa4989564"));

    public EmptyPage Create(EmptyPageParameter parameter)
    {
        return new EmptyPage
        {
            ViewModel = null,
            PageData = new EmptyPageData
            {
                FactoryId = Id
            }
        };
    }
}
