namespace NexusMods.Games.AdvancedInstaller.UI;

public class AdvancedInstallerWindowDesignViewModel : AdvancedInstallerWindowViewModel
{
    public AdvancedInstallerWindowDesignViewModel() : base(
        "Design Mod Name",
        DesignTimeHelpers.CreateDesignFileTree(),
        DesignTimeHelpers.CreateDesignGameLocationsRegister(),
        "Design Game Name")
    {
        CurrentPageVM = AdvancedInstallerVM;
    }
}
