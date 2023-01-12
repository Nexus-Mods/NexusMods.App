using System.Reflection;

namespace NexusMods.DataModel.JsonConverters.ExpressionGenerator;

public class AssemblyTypeFinder : ITypeFinder
{
    private readonly Assembly _assembly;
    public AssemblyTypeFinder(Assembly assembly)
    {
        _assembly = assembly;
    }

    public IEnumerable<Type> DescendentsOf(Type type)
    {
        var baseTypes = _assembly.GetTypes().Where(t => t.IsAssignableTo(type));

        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            var subtypes = _assembly.GetTypes()
                .Where(t => t.IsGenericTypeDefinition &&
                            t.GetInterfaces().Any(i => i.Name == definition.Name && i.Namespace == definition.Namespace) &&
                            t.GetGenericArguments().Length == type.GetGenericArguments().Length);
            baseTypes = baseTypes.Concat(subtypes.Select(s => s.MakeGenericType(type.GetGenericArguments())).ToArray());
        }
        
        return baseTypes;
    }
}