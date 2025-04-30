using System.Reflection;

namespace NexusMods.Abstractions.Serialization.ExpressionGenerator;

/// <summary>
/// An implementation of <see cref="ITypeFinder"/> that finds all types
/// in a given assembly.
/// </summary>
public class AssemblyTypeFinder : ITypeFinder
{
    private readonly Assembly _assembly;

    /// <summary>
    /// Creates an <see cref="AssemblyTypeFinder"/> that finds polymorphically
    /// assignable types for the JSON converter.
    /// </summary>
    /// <param name="assembly">The assembly to find the types in.</param>
    public AssemblyTypeFinder(Assembly assembly)
    {
        _assembly = assembly;
    }

    /// <inheritdoc />
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
