using System.ComponentModel;
using Avalonia;
using JetBrains.Annotations;
using R3;

namespace NexusMods.UI.Sdk;

/// <summary>
/// <see cref="Avalonia.Controls.UserControl"/> with R3 support.
/// </summary>
/// <remarks>Inherits from <see cref="Avalonia.ReactiveUI.ReactiveUserControl{TViewModel}"/> for compatibility.</remarks>
[PublicAPI]
public class R3UserControl<TViewModel> : Avalonia.ReactiveUI.ReactiveUserControl<TViewModel>, IR3View<TViewModel>
    where TViewModel : class, INotifyPropertyChanged
{
    /// <inheritdoc/>
    public BindableReactiveProperty<TViewModel?> BindableViewModel { get; } = new();

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property != DataContextProperty) return;
        if (!ReferenceEquals(change.OldValue, BindableViewModel.Value)) return;

        if (change.NewValue is null) BindableViewModel.Value = null;
        if (change.NewValue is TViewModel viewModel) BindableViewModel.Value = viewModel;
    }
}
