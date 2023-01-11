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
        return _assembly.GetTypes().Where(t => t.IsAssignableTo(type));
    }
}