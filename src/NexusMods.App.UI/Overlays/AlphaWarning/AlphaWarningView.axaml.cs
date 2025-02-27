using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using ReactiveUI;

namespace NexusMods.App.UI.Overlays.AlphaWarning;

[UsedImplicitly]
public partial class AlphaWarningView : ReactiveUserControl<IAlphaWarningViewModel>
{
    public AlphaWarningView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.BindCommand(ViewModel, vm => vm.OpenDiscordCommand, view => view.OpenDiscordButton)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.OpenForumsCommand, view => view.OpenForumsButton)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.OpenGitHubCommand, view => view.OpenGitHubButton)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.CloseCommand, view => view.DoneButton)
                .DisposeWith(disposables);
        });
    }
}

