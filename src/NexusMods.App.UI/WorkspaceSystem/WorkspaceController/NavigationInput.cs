using System.Diagnostics;
using JetBrains.Annotations;
using OneOf;
using MouseButton = Avalonia.Input.MouseButton;
using KeyboardKey = Avalonia.Input.Key;
using KeyboardModifiers = Avalonia.Input.KeyModifiers;

namespace NexusMods.App.UI.WorkspaceSystem;
using InputType = OneOf<MouseButton, KeyboardKey>;
using ModifierType = KeyboardModifiers;

[DebuggerDisplay("Modifier={Modifiers} Input={Input}")]
[PublicAPI]
public readonly struct NavigationInput : IEquatable<NavigationInput>
{
    public readonly InputType Input;
    public readonly ModifierType Modifiers;

    public NavigationInput(InputType input, ModifierType modifiers)
    {
        Input = input;
        Modifiers = modifiers;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Modifiers} + {Input}";
    }

    /// <inheritdoc/>
    public bool Equals(NavigationInput other)
    {
        return Input.Equals(other.Input) && Modifiers == other.Modifiers;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is NavigationInput other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(Input, Modifiers);
    }

    public static bool operator ==(NavigationInput left, NavigationInput right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(NavigationInput left, NavigationInput right)
    {
        return !(left == right);
    }
}

