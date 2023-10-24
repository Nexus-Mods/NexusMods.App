using NexusMods.DataModel.JsonConverters;

namespace NexusMods.App.UI.WorkspaceSystem;

public class EmptyPage : IPage
{
    public IViewModel? ViewModel { get; set; }

    public PageData PageData { get; set; } = new()
    {
        FactoryId = PageFactoryId.From(Guid.Parse("38d7c953-c237-49bb-8da4-319fa4989564")),
        Parameter = new EmptyPageParameter()
    };
}

[JsonName("NexusMods.App.UI.WorkspaceSystem.EmptyPageParameter")]
public record EmptyPageParameter : IPageFactoryParameter;

public class EmptyPageFactory : IPageFactory<EmptyPage, EmptyPageParameter>
{
    public PageFactoryId Id => PageFactoryId.From(Guid.Parse("38d7c953-c237-49bb-8da4-319fa4989564"));

    public EmptyPage Create(EmptyPageParameter parameter, PageData pageData)
    {
        return new EmptyPage
        {
            ViewModel = null,
            PageData = pageData
        };
    }
}
