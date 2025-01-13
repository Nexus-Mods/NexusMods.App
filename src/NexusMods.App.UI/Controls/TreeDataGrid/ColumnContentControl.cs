using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.Templates;
using ObservableCollections;
using R3;

namespace NexusMods.App.UI.Controls;

/// <summary>
/// Control for columns where row models are <see cref="CompositeItemModel{TKey}"/>.
/// </summary>
public class ColumnContentControl<TKey> : ContentControl
    where TKey : notnull
{
    [SuppressMessage("ReSharper", "CollectionNeverUpdated.Global", Justification = "Updated in XAML")]
    public List<DataTemplate> AvailableTemplates { get; } = [];

    public Control? Fallback { get; set; }

    private readonly SerialDisposable _serialDisposable = new();

    /// <summary>
    /// Builds a control from the first template in <see cref="AvailableTemplates"/>
    /// that matches with a component in the item model.
    /// </summary>
    private Control? BuildContent(CompositeItemModel<TKey> itemModel)
    {
        foreach (var template in AvailableTemplates)
        {
            var type = template.DataType;
            if (type is null) throw new UnreachableException();

            // NOTE(erri120): this could use a debug check to make sure that
            // the type specified in the data template is actually a component
            if (!itemModel.Components.TryGetValue(type, out var component)) continue;

            // NOTE(erri120): DataTemplate.Build doesn't use the
            // data you give it, need to manually set the DataContext.
            // Otherwise. the new control will inherit the parent context,
            // which is CompositeItemModel<TKey>.
            var control = template.Build(data: null);
            if (control is null) return null;

            control.DataContext = component;
            return control;
        }

        return Fallback;
    }

    private IDisposable Subscribe(CompositeItemModel<TKey> itemModel)
    {
        return itemModel.Components
            .ObserveChanged()
            .ObserveOnUIThreadDispatcher()
            .Subscribe((this, itemModel), static (_, state) =>
            {
                var (self, itemModel) = state;
                if (self.Presenter is null) return;

                var content = self.BuildContent(itemModel);
                self.Presenter.Content = content;
            });
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ContentProperty)
        {
            if (change.NewValue is not CompositeItemModel<TKey> itemModel)
            {
                _serialDisposable.Disposable = null;
                return;
            }

            if (IsLoaded) _serialDisposable.Disposable = Subscribe(itemModel);
        }
    }

    protected override bool RegisterContentPresenter(ContentPresenter presenter)
    {
        var didRegister = base.RegisterContentPresenter(presenter);
        if (didRegister && Content is CompositeItemModel<TKey> itemModel)
        {
            var content = BuildContent(itemModel);
            presenter.Content = content;
        }

        return didRegister;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (Content is not CompositeItemModel<TKey> itemModel)
        {
            _serialDisposable.Disposable = null;
            return;
        }

        Debug.Assert(_serialDisposable.Disposable is null, "nothing should've subscribed yet");
        _serialDisposable.Disposable = Subscribe(itemModel);
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        _serialDisposable.Disposable = null;
    }
}
