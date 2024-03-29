using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.CrossPlatform.Process;

namespace NexusMods.App.UI.Pages.Diagnostics;

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
            ServiceProvider.GetRequiredService<IOSInterop>(),
            WindowManager, 
            ServiceProvider.GetRequiredService<IDiagnosticWriter>(), 
            context.Diagnostic);
    }
}
