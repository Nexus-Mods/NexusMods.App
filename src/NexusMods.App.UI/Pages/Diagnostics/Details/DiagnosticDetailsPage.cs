using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.Diagnostics;

[JsonName("NexusMods.App.UI.Pages.Diagnostics.DiagnosticDetailsPageContext")]
public record DiagnosticDetailsPageContext : IPageFactoryContext
{
    public required Diagnostic Diagnostic { get; init; }
}

[UsedImplicitly]
public class DiagnosticDetailsPageFactory : APageFactory<IDiagnosticDetailsViewModel, DiagnosticDetailsPageContext>
{
    public static readonly PageFactoryId StaticId = PageFactoryId.From(Guid.Parse("96A85EAB-3748-4D30-8212-7A09CCDA225B"));
    
    public override PageFactoryId Id => StaticId;
    
    public DiagnosticDetailsPageFactory(IServiceProvider serviceProvider) : base(serviceProvider) { }
    
    public override IDiagnosticDetailsViewModel CreateViewModel(DiagnosticDetailsPageContext context)
    {
        return new DiagnosticDetailsViewModel(
            WindowManager,
            ServiceProvider.GetRequiredService<IDiagnosticWriter>(),
            ServiceProvider.GetRequiredService<IMarkdownRendererViewModel>(),
            context.Diagnostic
        );
    }
}
