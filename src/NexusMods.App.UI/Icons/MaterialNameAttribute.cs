namespace NexusMods.App.UI.Icons;

[AttributeUsage(AttributeTargets.Field)]
public class MaterialNameAttribute : Attribute
{
    public MaterialNameAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; }

}
