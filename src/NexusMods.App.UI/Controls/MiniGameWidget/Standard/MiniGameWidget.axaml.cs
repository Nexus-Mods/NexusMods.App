using System.Reactive;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Media;
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

                this.WhenAnyValue(view => view.ViewModel!.Name)
                    .Subscribe(name =>
                        {
                            NameTextBlock.Text = name;
                            
                            // set tip if the name is too long
                            var isTrimmed = NameTextBlock.TextTrimming != TextTrimming.None && NameTextBlock.TextLayout.TextLines.Any(x => x.HasCollapsed);
                            ToolTip.SetTip(NameTextBlock, isTrimmed ? name : null);
                        }
                    )
                    .DisposeWith(d);
                
                this.BindCommand<MiniGameWidget, IMiniGameWidgetViewModel, ReactiveCommand<Unit, Unit>, StandardButton>(ViewModel, vm => vm.GiveFeedbackCommand, view => view.ButtonGameNotFound)
                    .DisposeWith(d);

                this.WhenAnyValue(view => view.ViewModel!.GameInstallations)
                    .Subscribe(installations =>
                        {
                            if (installations is null)
                                return;

                            var tooltip = string.Join(", ", installations.Select(installation => installation.Store.ToString()));
                        }
                    )
                    .DisposeWith(d);
            }
        );
    }
}
