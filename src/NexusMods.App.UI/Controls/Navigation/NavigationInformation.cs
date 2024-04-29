using DynamicData.Kernel;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Controls.Navigation;

public record NavigationInformation(
    NavigationInput Input,
    Optional<OpenPageBehaviorType> OpenPageBehaviorType
)
{
    public static NavigationInformation From(OpenPageBehaviorType openPageBehaviorType)
    {
        return new NavigationInformation(
            Input: NavigationInput.Default,
            OpenPageBehaviorType: openPageBehaviorType
        );
    }

    public static NavigationInformation From(NavigationInput input)
    {
        return new NavigationInformation(
            Input: input,
            OpenPageBehaviorType: Optional<OpenPageBehaviorType>.None
        );
    }
}
