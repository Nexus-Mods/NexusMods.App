using NexusMods.Abstractions.Diagnostics;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.Diagnostics;

public class DiagnosticDetailsDesignViewModel : APageViewModel<IDiagnosticDetailsViewModel>, IDiagnosticDetailsViewModel
{
    private const string Details = "This is an example diagnostic details, lots of stuff here.";
    public DiagnosticSeverity Severity => DiagnosticSeverity.Critical;

    public IMarkdownRendererViewModel MarkdownRendererViewModel => new MarkdownRendererViewModel
    {
        Contents = Details
    };

    public DiagnosticDetailsDesignViewModel() : base(new DesignWindowManager()) { }

}
