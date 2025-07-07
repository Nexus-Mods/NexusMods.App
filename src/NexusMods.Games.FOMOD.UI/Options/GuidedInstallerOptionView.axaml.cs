using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using NexusMods.App.UI;
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
                        OptionNameTextBlock.Classes.Remove("StatusDangerLighter");
                    }
                    else
                    {
                        OptionNameTextBlock.Classes.Add("StatusDangerLighter");
                    }
                })
                .DisposeWith(disposables);
        });
    }

    private void PopulateFromViewModel(IGuidedInstallerOptionViewModel viewModel, bool useRadioButton)
    {
        OptionNameTextBlock.Text = viewModel.Option.Name;

        if (useRadioButton)
        {
            RadioButton.IsVisible = true;
            CheckBox.IsVisible = false;

            RadioButton.GroupName = viewModel.Group.Id.ToString();
        }
        else
        {
            CheckBox.IsVisible = true;
            RadioButton.IsVisible = false;
        }

        ImageIcon.IsVisible = viewModel.Option.Image is not null;
        DescriptionIcon.IsVisible = viewModel.Option.Description is not null;
    }
}

