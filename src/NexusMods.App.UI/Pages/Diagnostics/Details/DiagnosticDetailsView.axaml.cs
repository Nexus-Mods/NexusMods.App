using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media;
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
            /* NOTE(insomnious): Using fixed colors unless Laurence uses these colors\opacities again.
             * Same for the fixed text strings while we are testing the UI
             */
            
            case DiagnosticSeverity.Suggestion:
                SeverityTitleTextBlock.Text = Language.DiagnosticDetailsView_SeverityTitle_SUGGESTION;
                SeverityTitleTextBlock.Classes.Add("ForegroundInfoStrong");
                SeverityExplanationTextBlock.Text = "Suggestions may offer improvements to your experience.";
                MarkdownWrapperBorder.Background = SolidColorBrush.Parse("#0D93C5FD");
                MarkdownWrapperBorder.BorderBrush = SolidColorBrush.Parse("#6693C5FD");
                break;
            case DiagnosticSeverity.Warning:
                SeverityTitleTextBlock.Text = Language.DiagnosticDetailsView_SeverityTitle_WARNING;
                SeverityTitleTextBlock.Classes.Add("ForegroundWarningStrong");
                SeverityExplanationTextBlock.Text = "Warnings may negatively impact your experience.";
                MarkdownWrapperBorder.Background = SolidColorBrush.Parse("#0DFEF08A"); 
                MarkdownWrapperBorder.BorderBrush = SolidColorBrush.Parse("#66FEF08A");
                break;
            case DiagnosticSeverity.Critical:
                SeverityTitleTextBlock.Text = Language.DiagnosticDetailsView_SeverityTitle_CRITICAL_ERROR;
                SeverityExplanationTextBlock.Text = "Critical errors make the game unplayable.";
                SeverityTitleTextBlock.Classes.Add("ForegroundDangerStrong");
                MarkdownWrapperBorder.Background = SolidColorBrush.Parse("#0DF87171");
                MarkdownWrapperBorder.BorderBrush = SolidColorBrush.Parse("#66F87171");
                break;
            default:
                SeverityTitleTextBlock.Text = Language.DiagnosticDetailsView_SeverityTitle_HIDDEN;
                break;
        }
    }
}
