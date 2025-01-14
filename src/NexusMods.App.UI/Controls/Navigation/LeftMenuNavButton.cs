using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using JetBrains.Annotations;

namespace NexusMods.App.UI.Controls.Navigation;

/// <summary>
/// A standard left menu button with active and selected states, as well as workspace navigation capabilities.
/// </summary>
[PublicAPI]
[PseudoClasses(pcActive, pcSelected)]
public class LeftMenuNavButton : NavigationControl
{
    private const string pcActive = ":active";
    private const string pcSelected = ":selected";
    
    /// <summary>
    /// Defines the <see cref="IsActive"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsActiveProperty =
        AvaloniaProperty.Register<LeftMenuNavButton, bool>(nameof(IsActive), false);
    
    /// <summary>
    /// Defines the <see cref="IsSelected"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<LeftMenuNavButton, bool>(nameof(IsSelected), false);
    
    /// <summary>
    /// Gets or sets whether the button is in the active state.
    /// </summary>
    public bool IsActive
    {
        get => GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }
    
    /// <summary>
    /// Gets or sets whether the button is in the selected state.
    /// </summary>
    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }
    
    /// <inheritdoc/>
    /// Parent class <see cref="NavigationControl"/> has a StyleKey of <see cref="StandardButton"/>,
    /// which would prevent this class from having specialized styling.
    protected override Type StyleKeyOverride => typeof(LeftMenuNavButton);
    
    
    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        switch (change.Property.Name)
        {
            case nameof(IsActive):
                PseudoClasses.Set(pcActive, IsActive);
                break;
            case nameof(IsSelected):
                PseudoClasses.Set(pcSelected, IsSelected);
                break;
        }
    }

}
