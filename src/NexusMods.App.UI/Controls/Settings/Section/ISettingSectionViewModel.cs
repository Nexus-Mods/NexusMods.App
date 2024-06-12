using NexusMods.Abstractions.Settings;

namespace NexusMods.App.UI.Controls.Settings.Section;

public interface ISettingSectionViewModel : IViewModelInterface
{
    ISettingsSectionDescriptor Descriptor { get; }
}
