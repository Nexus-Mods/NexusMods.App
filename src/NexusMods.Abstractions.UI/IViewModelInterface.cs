using System.ComponentModel;
using ReactiveUI;

namespace NexusMods.Abstractions.UI;

/// <summary>
/// Marker interface for view model interfaces.
/// </summary>
public interface IViewModelInterface : INotifyPropertyChanged
{
    public ViewModelActivator Activator { get; }
}
