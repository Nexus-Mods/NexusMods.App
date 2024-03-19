using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.App.UI.Resources;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Diagnostics;

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
        });
    }
    
    private void InitializeData(IDiagnosticDetailsViewModel vm)
    {
        switch (vm.Severity)
        {
            case DiagnosticSeverity.Suggestion:
                SeverityIcon.Classes.Add("HelpCircle");
                SeverityIcon.Classes.Add("ForegroundInfoStrong");
                SeverityTitleTextBlock.Text = Language.DiagnosticDetailsView_SeverityTitle_SUGGESTION;
                SeverityTitleTextBlock.Classes.Add("ForegroundInfoStrong");
                break;
            case DiagnosticSeverity.Warning:
                SeverityIcon.Classes.Add("Alert");
                SeverityIcon.Classes.Add("ForegroundWarningStrong");
                SeverityTitleTextBlock.Text = Language.DiagnosticDetailsView_SeverityTitle_WARNING;
                SeverityTitleTextBlock.Classes.Add("ForegroundWarningStrong");
                break;
            case DiagnosticSeverity.Critical:
                SeverityIcon.Classes.Add("AlertOctagon");
                SeverityIcon.Classes.Add("ForegroundDangerStrong");
                SeverityTitleTextBlock.Text = Language.DiagnosticDetailsView_SeverityTitle_CRITICAL_ERROR;
                SeverityTitleTextBlock.Classes.Add("ForegroundDangerStrong");
                break;
            default:
                SeverityIcon.Classes.Add("Bell");
                SeverityTitleTextBlock.Text = Language.DiagnosticDetailsView_SeverityTitle_HIDDEN;
                break;
        }
        
        DetailsTextBlock.Text = vm.Details;
    }
}

