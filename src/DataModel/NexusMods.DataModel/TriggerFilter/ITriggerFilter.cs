using NexusMods.Hashing.xxHash64;

namespace NexusMods.DataModel.TriggerFilter;

/// <summary>
/// A filter for limiting when a trigger is executed. Triggers are added to files to signify to the application that
/// the contents of an entity should be recalculated when the inputs are changed. Instead of recalculating the entity
/// on every single change, the application can use a trigger filter to limit the number of recalculations.
/// </summary>
/// <typeparam name="TInput">Input data to the filter</typeparam>
/// <typeparam name="TSelf">A Id or marker object to give the filter context as to what is being filtered</typeparam>
public interface ITriggerFilter<in TSelf, in TInput>
{
    /// <summary>
    /// Get a (locally) unique hash of the input. This need not be globally unique, but it should be unique enough that
    /// a change in the parts of the input that concern the filter will result in a different hash.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    public Hash GetFingerprint(TSelf self, TInput input);
}
