using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using NexusMods.Common.GuidedInstaller;
using ReactiveUI;

namespace NexusMods.Games.FOMOD.UI;

public partial class GuidedInstallerOptionView : ReactiveUserControl<IGuidedInstallerOptionViewModel>
{
    public GuidedInstallerOptionView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            OptionNameTextBlock.Text = ViewModel?.Option.Name;

            var groupType = ViewModel?.Group.Type ?? OptionGroupType.Any;
            var useRadioButton = groupType switch
            {
                OptionGroupType.ExactlyOne => true,
                OptionGroupType.AtMostOne => true,
                _ => false
            };

            if (useRadioButton)
            {
                RadioButton.IsVisible = true;
                CheckBox.IsVisible = false;

                RadioButton.GroupName = ViewModel?.Group.Id.ToString() ?? Guid.NewGuid().ToString();

                this.OneWayBind(ViewModel, vm => vm.IsEnabled, view => view.RadioButton.IsEnabled)
                    .DisposeWith(disposables);

                this.Bind(ViewModel, vm => vm.IsChecked, view => view.RadioButton.IsChecked)
                    .DisposeWith(disposables);
            }
            else
            {
                CheckBox.IsVisible = true;
                RadioButton.IsVisible = false;

                this.OneWayBind(ViewModel, vm => vm.IsEnabled, view => view.CheckBox.IsEnabled)
                    .DisposeWith(disposables);

                this.Bind(ViewModel, vm => vm.IsChecked, view => view.CheckBox.IsChecked)
                    .DisposeWith(disposables);
            }

            ImageIcon.IsVisible = ViewModel?.Option.ImageUrl is not null;
            DescriptionIcon.IsVisible = ViewModel?.Option.Description is not null;

            var hoverText = ViewModel?.Option.HoverText;
            if (hoverText is not null)
            {
                ToolTip.SetTip(OptionNameTextBlock, hoverText);
            }
        });
    }
}

