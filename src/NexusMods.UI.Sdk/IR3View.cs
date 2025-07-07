using System.ComponentModel;
using JetBrains.Annotations;
using R3;

namespace NexusMods.UI.Sdk;

/// <summary>
/// Represents a View with R3 support for the View Model.
/// </summary>
/// <remarks>Implements <see cref="ReactiveUI.IViewFor{TViewModel}"/> for compatibility.</remarks>
[PublicAPI]
public interface IR3View<TViewModel> : ReactiveUI.IViewFor<TViewModel>, INotifyPropertyChanged
    where TViewModel : class, INotifyPropertyChanged
{
    /// <summary>
    /// Reactive property containing the View Model.
    /// </summary>
    BindableReactiveProperty<TViewModel?> BindableViewModel { get; }
}
