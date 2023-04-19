using System.Reflection;

namespace NexusMods.App.UI.ViewModels.Helpers.ViewModelSelector;

public abstract class AViewModelAttribute : Attribute
{ 
    public abstract Type ViewModelType { get; }
    
    public static Dictionary<TEnum, Type> GetAttributes<TEnum>() where TEnum : struct, Enum
    {
        var values = new Dictionary<TEnum, Type>();

        foreach (var value in Enum.GetValues<TEnum>())
        {
            var name = Enum.GetName(value)!;
            var attribute =
                typeof(TEnum)
                    .GetMember(name)
                    .First(member => member.MemberType == MemberTypes.Field)
                    .GetCustomAttributes(typeof(AViewModelAttribute), false)
                    .OfType<AViewModelAttribute>()
                    .Single();
            values.Add(value, attribute.ViewModelType);
        }

        return values;
    }
}

[AttributeUsage(AttributeTargets.Enum, AllowMultiple = false)]
public class ViewModelAttribute<T> : AViewModelAttribute where T : IViewModelInterface
{
    public override Type ViewModelType => typeof(T);

    


}
