using JetBrains.Annotations;
using OneOf;

namespace NexusMods.Abstractions.GuidedInstallers;

/// <summary>
/// Represents the user's choice in a guided installer.
/// </summary>
[PublicAPI]
public sealed class UserChoice : OneOfBase<
    UserChoice.CancelInstallation,
    UserChoice.GoToPreviousStep,
    UserChoice.GoToNextStep>
{
    /// <summary>
    /// The user wants to cancel the installation.
    /// </summary>
    public sealed record CancelInstallation;

    /// <summary>
    /// The user wants to go to the previous installation step.
    /// </summary>
    public sealed record GoToPreviousStep;

    /// <summary>
    /// The user wants to go to the next installation step.
    /// </summary>
    /// <param name="SelectedOptions">The options the user has selected.</param>
    public sealed record GoToNextStep(SelectedOption[] SelectedOptions);

    /// <summary>
    /// Constructor.
    /// </summary>
    public UserChoice(OneOf<CancelInstallation, GoToPreviousStep, GoToNextStep> input) : base(input) { }

    /// <summary>
    /// <see cref="IsCancelInstallation"/>
    /// </summary>
    public bool IsCancelInstallation => IsT0;

    /// <summary>
    /// <see cref="IsGoToPreviousStep"/>
    /// </summary>
    public bool IsGoToPreviousStep => IsT1;

    /// <summary>
    /// <see cref="IsGoToNextStep"/>
    /// </summary>
    public bool IsGoToNextStep => IsT2;
}
