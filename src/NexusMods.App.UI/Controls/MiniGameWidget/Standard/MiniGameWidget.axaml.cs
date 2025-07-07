using System.Reactive;
using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.MiniGameWidget.Standard;

public partial class MiniGameWidget : ReactiveUserControl<IMiniGameWidgetViewModel>
{
    public MiniGameWidget()
    {
        InitializeComponent();

        this.WhenActivated(d =>
            {
                this.OneWayBind(ViewModel, vm => vm.Image, v => v.GameImage.Source)
                    .DisposeWith(d);

                this.OneWayBind<IMiniGameWidgetViewModel, MiniGameWidget, string, string?>(ViewModel, vm => vm.Name, v => v.NameTextBlock.Text)
                    .DisposeWith(d);

                this.BindCommand<MiniGameWidget, IMiniGameWidgetViewModel, ReactiveCommand<Unit, Unit>, StandardButton>(ViewModel, vm => vm.GiveFeedbackCommand, view => view.ButtonGameNotFound)
                    .DisposeWith(d);
                
                this.WhenAnyValue(view => view.ViewModel!.GameInstallations)
                    .Subscribe(installations =>
                        {
                            if(installations is null)
                                return;
                            
                            var tooltip = string.Join(", ", installations.Select(installation => installation.Store.ToString()));
                        }
                    )
                    .DisposeWith(d);
            }
        );
    }
}
