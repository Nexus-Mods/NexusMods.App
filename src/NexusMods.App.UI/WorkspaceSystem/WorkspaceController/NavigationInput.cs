using System.Diagnostics;
using JetBrains.Annotations;
using OneOf;
using MouseButton = Avalonia.Input.MouseButton;
using KeyboardKey = Avalonia.Input.Key;
using KeyboardModifiers = Avalonia.Input.KeyModifiers;

namespace NexusMods.App.UI.WorkspaceSystem;
using KeyType = OneOf<MouseButton, KeyboardKey>;
using ModifierType = KeyboardModifiers;

[DebuggerDisplay("Modifier={Modifiers} Key={Key}")]
[PublicAPI]
public readonly struct NavigationInput : IEquatable<NavigationInput>
{
    public readonly KeyType Key;
    public readonly ModifierType Modifiers;

    public NavigationInput(KeyType key, ModifierType modifiers)
    {
        Key = key;
        Modifiers = modifiers;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Modifiers} + {Key}";
    }

    /// <inheritdoc/>
    public bool Equals(NavigationInput other)
    {
        return Key.Equals(other.Key) && Modifiers == other.Modifiers;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is NavigationInput other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(Key, Modifiers);
    }

    public static bool operator ==(NavigationInput left, NavigationInput right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(NavigationInput left, NavigationInput right)
    {
        return !(left == right);
    }

    public void Deconstruct(out KeyType key, out ModifierType modifiers)
    {
        key = Key;
        modifiers = Modifiers;
    }

    public bool IsPrimaryInput()
    {
        if (Modifiers != KeyboardModifiers.None) return false;

        return Key.Match(
            f0: mouseButton => mouseButton == MouseButton.Left,
            f1: keyboardKey => keyboardKey == KeyboardKey.Enter
        );
    }
}

