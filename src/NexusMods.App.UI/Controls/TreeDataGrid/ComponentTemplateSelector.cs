using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Metadata;
using ObservableCollections;
using R3;

namespace NexusMods.App.UI.Controls;

public class ComponentColumn<TKey> : ContentControl
    where TKey : notnull
{
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

            ContentChanged(itemModel);
        }
    }

    private readonly SerialDisposable _serialDisposable = new();
    private void ContentChanged(CompositeItemModel<TKey> itemModel)
    {
        var disposable = itemModel.Components
            .ObserveChanged()
            .ObserveOnUIThreadDispatcher()
            .Subscribe((this, itemModel), static (_, state) =>
        {
            var (self, itemModel) = state;

            foreach (var template in self.DataTemplates)
            {
                if (template is not ComponentTemplateSelector<TKey> selector) continue;
                if (self.Presenter is null) return;
                self.Presenter.Content = selector.Build(itemModel);
                return;
            }
        });

        _serialDisposable.Disposable = disposable;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (_serialDisposable.Disposable is not null) return;
        if (Content is not CompositeItemModel<TKey> itemModel) return;
        ContentChanged(itemModel);
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        _serialDisposable.Disposable = null;
    }
}

public class ComponentTemplateSelector<TKey> : IDataTemplate
    where TKey : notnull
{
    [Content]
    [SuppressMessage("ReSharper", "CollectionNeverUpdated.Global", Justification = "Updated in XAML")]
    public List<DataTemplate> AvailableTemplates { get; } = [];

    public Control? Fallback { get; set; }

    public Control? Build(object? param)
    {
        if (param is not CompositeItemModel<TKey> itemModel) throw new UnreachableException();

        foreach (var kv in itemModel.Components)
        {
            var (_, component) = kv;
            foreach (var template in AvailableTemplates)
            {
                if (template.DataType is null) throw new UnreachableException();
                if (!template.DataType.IsInstanceOfType(component)) continue;

                // NOTE(erri120): DataTemplate.Build doesn't use the
                // data you give it, need to manually set the DataContext.
                // Otherwise. the new control will inherit the parent context,
                // which is CompositeItemModel<TKey>.
                var control = template.Build(data: null);
                if (control is null) return null;

                control.DataContext = component;
                return control;
            }
        }

        return Fallback;
    }

    public bool Match(object? data)
    {
        return data is CompositeItemModel<TKey>;
    }
}
