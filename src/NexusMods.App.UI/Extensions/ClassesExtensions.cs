using Avalonia.Controls;

namespace NexusMods.App.UI.Extensions;

public static class ClassesExtensions
{
    public static void ToggleIf(this Classes classes, string name, bool condition)
    {
        if (condition) classes.Add(name);
        else classes.Remove(name);
    }

    public static void Toggle(this Classes classes, string name)
    {
        if (!classes.Remove(name)) classes.Add(name);
    }
}
