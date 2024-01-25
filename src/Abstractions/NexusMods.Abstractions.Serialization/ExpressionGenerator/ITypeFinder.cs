namespace NexusMods.Abstractions.Serialization.ExpressionGenerator;

/// <summary>
/// This type finder is necessary for serialization/deserialization of items
/// using the polymorphic serialization system implemented in the <see cref="JsonConverters"/>
/// namespace.
/// </summary>
public interface ITypeFinder
{
    /// <summary>
    /// Finds all descendents of a given type, i.e. types which are assignable
    /// to the specified type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    public IEnumerable<Type> DescendentsOf(Type type);
}
