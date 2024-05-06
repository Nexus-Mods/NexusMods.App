using System.Collections.ObjectModel;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
/// A repository for a model, that allows retrival of models
/// via a variety of methods.
/// </summary>
/// <typeparam name="TModel"></typeparam>
public interface IRepository<TModel> where TModel : Entity
{
    /// <summary>
    /// All models in the repository as an enumerable.
    /// </summary>
    /// <returns></returns>
    IEnumerable<TModel> All { get; }
    
    /// <summary>
    /// All models in the repository as an observable collection. As models
    /// are modified, added, or removed, the observable collection will update.
    /// </summary>
    ReadOnlyObservableCollection<TModel> Observable { get; }
}
