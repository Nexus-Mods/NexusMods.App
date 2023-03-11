using ReactiveUI;

namespace NexusMods.App.UI.Controls.Spine.Buttons.Icon;

public class IconButtonDesignViewModel : IconButtonViewModel
{
    public IconButtonDesignViewModel()
    {
        Click = ReactiveCommand.Create(() => { IsActive = !IsActive; });
    }

}
