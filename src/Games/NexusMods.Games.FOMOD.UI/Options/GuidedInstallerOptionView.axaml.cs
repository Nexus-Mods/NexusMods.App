using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using NexusMods.App.UI;
using NexusMods.Common.GuidedInstaller;
using ReactiveUI;

namespace NexusMods.Games.FOMOD.UI;

[UsedImplicitly]
public partial class GuidedInstallerOptionView : ReactiveUserControl<IGuidedInstallerOptionViewModel>
{
    public GuidedInstallerOptionView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            var useRadioButton = ViewModel!.Group.Type.UsesRadioButtons();
            PopulateFromViewModel(ViewModel!, useRadioButton);

            if (useRadioButton)
            {
                this.OneWayBind(ViewModel, vm => vm.IsEnabled, view => view.RadioButton.IsEnabled)
                    .DisposeWith(disposables);

                this.Bind(ViewModel, vm => vm.IsChecked, view => view.RadioButton.IsChecked)
                    .DisposeWith(disposables);
            }
            else
            {
                this.OneWayBind(ViewModel, vm => vm.IsEnabled, view => view.CheckBox.IsEnabled)
                    .DisposeWith(disposables);

                this.Bind(ViewModel, vm => vm.IsChecked, view => view.CheckBox.IsChecked)
                    .DisposeWith(disposables);
            }

            this.WhenAnyValue(x => x.ViewModel!.IsValid)
                .SubscribeWithErrorLogging(isValid =>
                {
                    if (isValid)
                    {
                        RadioButtonTextBlock.Classes.Remove("StatusDangerLighter");
                        CheckBoxTextBlock.Classes.Remove("StatusDangerLighter");
                    }
                    else
                    {
                        RadioButtonTextBlock.Classes.Add("StatusDangerLighter");
                        CheckBoxTextBlock.Classes.Add("StatusDangerLighter");
                    }
                })
                .DisposeWith(disposables);
        });
    }

    private void PopulateFromViewModel(IGuidedInstallerOptionViewModel viewModel, bool useRadioButton)
    {
        if (useRadioButton)
        {
            RadioButtonTextBlock.Text = viewModel.Option.Name;

            RadioButton.IsVisible = true;
            CheckBox.IsVisible = false;

            RadioButton.GroupName = viewModel.Group.Id.ToString();
        }
        else
        {
            CheckBoxTextBlock.Text = viewModel.Option.Name;

            CheckBox.IsVisible = true;
            RadioButton.IsVisible = false;
        }

        ImageIcon.IsVisible = viewModel.Option.ImageUrl is not null;
        DescriptionIcon.IsVisible = viewModel.Option.Description is not null;
    }
}

