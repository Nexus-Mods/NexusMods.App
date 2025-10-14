using System.ComponentModel;
using JetBrains.Annotations;
using ReactiveUI;

namespace NexusMods.UI.Sdk;

/// <summary>
/// Marker interface for view model interfaces.
/// </summary>
[PublicAPI]
public interface IViewModelInterface : INotifyPropertyChanged
{
    public ViewModelActivator Activator { get; }
}
