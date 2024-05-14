using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.App.UI.Pages.TextEdit;

[JsonName("TextEditorPageContext")]
public record TextEditorPageContext : IPageFactoryContext
{
    public required Hash FileHash { get; init; }
    public required RelativePath FileName { get; init; }
}

[UsedImplicitly]
public class TextEditorPageFactory : APageFactory<ITextEditorPageViewModel, TextEditorPageContext>
{
    public TextEditorPageFactory(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public static readonly PageFactoryId StaticId = PageFactoryId.From(Guid.Parse("ea4947ea-5f97-4e4d-abf2-bd511fbd9b4e"));
    public override PageFactoryId Id => StaticId;

    public override ITextEditorPageViewModel CreateViewModel(TextEditorPageContext context)
    {
        var vm = ServiceProvider.GetRequiredService<ITextEditorPageViewModel>();
        vm.Context = context;
        return vm;
    }
}
