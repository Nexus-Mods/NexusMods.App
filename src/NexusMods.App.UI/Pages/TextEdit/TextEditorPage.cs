using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.TextEdit;

public record TextEditorPageContext : IPageFactoryContext;

[UsedImplicitly]
public class TextEditorPageFactory : APageFactory<ITextEditorPageViewModel, TextEditorPageContext>
{
    public TextEditorPageFactory(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public static readonly PageFactoryId StaticId = PageFactoryId.From(Guid.Parse("ea4947ea-5f97-4e4d-abf2-bd511fbd9b4e"));
    public override PageFactoryId Id => StaticId;

    public override ITextEditorPageViewModel CreateViewModel(TextEditorPageContext context)
    {
        var vm = ServiceProvider.GetRequiredService<ITextEditorPageViewModel>();
        return vm;
    }
}
