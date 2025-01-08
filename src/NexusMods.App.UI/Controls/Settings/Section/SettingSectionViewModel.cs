using NexusMods.Abstractions.Settings;
using NexusMods.Abstractions.UI;

namespace NexusMods.App.UI.Controls.Settings.Section;

public class SettingSectionViewModel : AViewModel<ISettingSectionViewModel>, ISettingSectionViewModel
{
    public ISettingsSectionDescriptor Descriptor { get; }

    public SettingSectionViewModel(ISettingsSectionDescriptor descriptor)
    {
        Descriptor = descriptor;
    }
}
