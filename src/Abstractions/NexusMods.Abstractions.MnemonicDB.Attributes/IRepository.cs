using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
/// A repository for a model, that allows retrival of models
/// via a variety of methods.
/// </summary>
/// <typeparam name="TModel"></typeparam>
public interface IRepository<TModel>
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

    /// <summary>
    /// Gets the revisions of a specific model.
    /// </summary>
    IObservable<TModel> Revisions(EntityId id, bool includeCurrent = true);

    /// <summary>
    /// True if the model exists in the repository
    /// </summary>
    /// <param name="loadoutId"></param>
    /// <returns></returns>
    bool Exists(EntityId loadoutId);

    /// <summary>
    /// Completely deletes a model from the repository, by retracting all attributes, returns the new db
    /// with the model removed.
    /// </summary>
    /// <param name="first"></param>
    Task Delete(TModel first);

    /// <summary>
    /// Tries to find the first model where the attribute matches the value.
    /// </summary>
    bool TryFindFirst<TOuter, TInner>(Attribute<TOuter, TInner> attr, TOuter value, [NotNullWhen(true)] out TModel? model);
    
    /// <summary>
    /// Tries to find the first model, returns false if no model is found.
    /// </summary>
    bool TryFindFirst([NotNullWhen(true)] out TModel? model);
    
    /// <summary>
    /// Finds all models where the attribute matches the value.
    /// </summary>
    /// <param name="attr"></param>
    /// <param name="value"></param>
    /// <typeparam name="TOuter"></typeparam>
    /// <typeparam name="TInner"></typeparam>
    /// <returns></returns>
    IEnumerable<TModel> FindAll<TOuter, TInner>(Attribute<TOuter, TInner> attr, TOuter value);
}
