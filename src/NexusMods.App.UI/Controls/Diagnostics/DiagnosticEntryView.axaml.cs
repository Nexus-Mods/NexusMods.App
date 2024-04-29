using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using NexusMods.Abstractions.Diagnostics;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Diagnostics;

public partial class DiagnosticEntryView : ReactiveUserControl<IDiagnosticEntryViewModel>
{
    public DiagnosticEntryView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(view => view.ViewModel)
                .WhereNotNull()
                .Do(InitializeData)
                .Subscribe()
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.SeeDetailsCommand, view => view.EntryButton)
                .DisposeWith(d);
        });
    }

    private void InitializeData(IDiagnosticEntryViewModel vm)
    {
        switch (vm.Severity)
        {
            case DiagnosticSeverity.Suggestion:
                SeverityIcon.Classes.Add("HelpCircle");
                SeverityIcon.Classes.Add("ForegroundInfoStrong");
                break;
            case DiagnosticSeverity.Warning:
                SeverityIcon.Classes.Add("Alert");
                SeverityIcon.Classes.Add("ForegroundWarningStrong");
                break;
            case DiagnosticSeverity.Critical:
                SeverityIcon.Classes.Add("AlertOctagon");
                SeverityIcon.Classes.Add("ForegroundDangerStrong");
                break;
            default:
                SeverityIcon.Classes.Add("Bell");
                break;
        }
                
        DescriptionText.Text = vm.Summary;
    }
}
