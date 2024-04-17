using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.App.UI.Resources;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Diagnostics;

[UsedImplicitly]
public partial class DiagnosticDetailsView : ReactiveUserControl<IDiagnosticDetailsViewModel>
{
    public DiagnosticDetailsView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
            {
                this.WhenAnyValue(view => view.ViewModel)
                    .WhereNotNull()
                    .Do(InitializeData)
                    .Subscribe()
                    .DisposeWith(d);
            }
        );
    }

    private void InitializeData(IDiagnosticDetailsViewModel vm)
    {
        MarkdownRendererViewModelViewHost.ViewModel = vm.MarkdownRendererViewModel;

        switch (vm.Severity)
        {
            case DiagnosticSeverity.Suggestion:
                SeverityIcon.Classes.Add("HelpCircle");
                SeverityIcon.Classes.Add("ForegroundInfoStrong");
                SeverityTitleTextBlock.Text = Language.DiagnosticDetailsView_SeverityTitle_SUGGESTION.ToUpperInvariant();
                SeverityTitleTextBlock.Classes.Add("ForegroundInfoStrong");
                HorizontalLine.Classes.Add("InfoStrong");
                break;
            case DiagnosticSeverity.Warning:
                SeverityIcon.Classes.Add("Alert");
                SeverityIcon.Classes.Add("ForegroundWarningStrong");
                SeverityTitleTextBlock.Text = Language.DiagnosticDetailsView_SeverityTitle_WARNING.ToUpperInvariant();
                SeverityTitleTextBlock.Classes.Add("ForegroundWarningStrong");
                HorizontalLine.Classes.Add("WarningStrong");
                break;
            case DiagnosticSeverity.Critical:
                SeverityIcon.Classes.Add("AlertOctagon");
                SeverityIcon.Classes.Add("ForegroundDangerStrong");
                SeverityTitleTextBlock.Text = Language.DiagnosticDetailsView_SeverityTitle_CRITICAL_ERROR.ToUpperInvariant();
                SeverityTitleTextBlock.Classes.Add("ForegroundDangerStrong");
                HorizontalLine.Classes.Add("DangerStrong");
                break;
            default:
                SeverityIcon.Classes.Add("Bell");
                SeverityTitleTextBlock.Text = Language.DiagnosticDetailsView_SeverityTitle_HIDDEN.ToUpperInvariant();
                break;
        }
    }
}
