using System.ComponentModel;

namespace NexusMods.UI.Theme.Controls.Spine.Buttons;

public interface ISpineButtonViewModel : INotifyPropertyChanged
{
    public bool IsActive { get; set; }
}