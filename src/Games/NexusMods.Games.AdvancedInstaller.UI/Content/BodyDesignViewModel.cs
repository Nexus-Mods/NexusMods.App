namespace NexusMods.Games.AdvancedInstaller.UI;

public class BodyDesignViewModel() : BodyViewModel(
    new DeploymentData(),
    "Design Mod Name",
    DesignTimeHelpers.CreateDesignFileTree(),
    DesignTimeHelpers.CreateDesignGameLocationsRegister(),
    null,
    "Design Game Name");
