using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Icons;
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
                SeverityIcon.Value = IconValues.InfoFilled;
                SeverityIcon.Classes.Add("ForegroundInfoStrong");
                break;
            case DiagnosticSeverity.Warning:
                SeverityIcon.Value = IconValues.Warning;
                SeverityIcon.Classes.Add("ForegroundWarningStrong");
                break;
            case DiagnosticSeverity.Critical:
                SeverityIcon.Value = IconValues.Error;
                SeverityIcon.Classes.Add("ForegroundDangerStrong");
                break;
            default:
                SeverityIcon.Value = IconValues.NotificationImportant;
                break;
        }
                
        DescriptionText.Text = vm.Summary;
        TitleText.Text = vm.Title;
    }
}
