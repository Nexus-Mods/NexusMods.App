namespace NexusMods.DataModel.JsonConverters.ExpressionGenerator;

public interface ITypeFinder
{
    public IEnumerable<Type> DescendentsOf(Type type);
}