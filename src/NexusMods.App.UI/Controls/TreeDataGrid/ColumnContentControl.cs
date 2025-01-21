using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.Templates;
using NexusMods.App.UI.Extensions;
using ObservableCollections;
using R3;

namespace NexusMods.App.UI.Controls;

public interface IComponentTemplate
{
    Type ComponentType { get; }

    ComponentKey ComponentKey { get; }

    DataTemplate DataTemplate { get; }
}

public class ComponentTemplate<TComponent> : IComponentTemplate
    where TComponent : class, IItemModelComponent<TComponent>, IComparable<TComponent>
{
    public Type ComponentType => typeof(TComponent);
    public ComponentKey ComponentKey { get; set; }

    // NOTE(erri120): Rider currently is unable to properly understand DataTypeAttribute.
    // The below is a hack and will be replaced in the future when Rider fixes their
    // inline hints. See the bug report for more details:
    // https://youtrack.jetbrains.com/issue/RIDER-121820

    private DataTemplate? _dataTemplate;
    public DataTemplate DataTemplate
    {
        get => _dataTemplate ?? throw new InvalidOperationException();
        set
        {
            if (value.DataType != ComponentType) throw new InvalidOperationException();
            _dataTemplate = value;
        }
    }
}

/// <summary>
/// Custom <see cref="ContentControl"/> to reactively build the control.
/// </summary>
public abstract class AReactiveContentControl<TContent> : ContentControl
    where TContent : class
{
    private readonly SerialDisposable _serialDisposable = new();

    /// <summary>
    /// Builds a control for the given content.
    /// </summary>
    protected abstract Control? BuildContentControl(TContent content);

    /// <summary>
    /// Subscribes to content changes.
    /// </summary>
    protected abstract IDisposable Subscribe(TContent content);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ContentProperty)
        {
            if (change.NewValue is not TContent content)
            {
                _serialDisposable.Disposable = null;
                return;
            }

            // NOTE(erri120): we only care about Content changes when the
            // Control is fully constructed and rendered on screen.
            if (IsLoaded) _serialDisposable.Disposable = Subscribe(content);
        }
    }

    protected override bool RegisterContentPresenter(ContentPresenter presenter)
    {
        var didRegister = base.RegisterContentPresenter(presenter);

        // NOTE(erri120): Puts content into the presenter before the first render.
        if (didRegister && Content is TContent content)
        {
            var control = BuildContentControl(content);
            presenter.Content = control;
        }

        return didRegister;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (Content is not TContent content)
        {
            _serialDisposable.Disposable = null;
            return;
        }

        Debug.Assert(_serialDisposable.Disposable is null, "nothing should've subscribed yet");
        _serialDisposable.Disposable = Subscribe(content);
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        _serialDisposable.Disposable = null;
    }
}

public class ComponentControl<TKey> : AReactiveContentControl<CompositeItemModel<TKey>>
    where TKey : notnull
{
    public IComponentTemplate? ComponentTemplate { get; set; }

    public Control? Fallback { get; set; }

    protected override Control? BuildContentControl(CompositeItemModel<TKey> itemModel)
    {
        if (ComponentTemplate is null) throw new InvalidOperationException();
        if (!itemModel.TryGet(ComponentTemplate.ComponentKey, ComponentTemplate.ComponentType, out var component)) return Fallback;

        // NOTE(erri120): DataTemplate.Build doesn't use the
        // data you give it, need to manually set the DataContext.
        // Otherwise. the new control will inherit the parent context,
        // which is CompositeItemModel<TKey>.
        var control = ComponentTemplate.DataTemplate.Build(data: null);
        if (control is null) return Fallback;

        control.DataContext = component;
        return control;
    }

    protected override IDisposable Subscribe(CompositeItemModel<TKey> itemModel)
    {
        if (ComponentTemplate is null) throw new InvalidOperationException();
        var key = ComponentTemplate.ComponentKey;

        return itemModel.Components
            .ObserveKeyChanges(key)
            .ObserveOnUIThreadDispatcher()
            .Subscribe((this, itemModel), static (_, state) =>
            {
                var (self, itemModel) = state;
                if (self.Presenter is null) return;

                var content = self.BuildContentControl(itemModel);
                self.Presenter.Content = content;
            });
    }
}

/// <summary>
/// Control for columns where row models are <see cref="CompositeItemModel{TKey}"/>.
/// </summary>
public class MultiComponentControl<TKey> : AReactiveContentControl<CompositeItemModel<TKey>>
    where TKey : notnull
{
    [SuppressMessage("ReSharper", "CollectionNeverUpdated.Global", Justification = "Updated in XAML")]
    public List<IComponentTemplate> AvailableTemplates { get; } = [];

    public Control? Fallback { get; set; }

    /// <summary>
    /// Builds a control from the first template in <see cref="AvailableTemplates"/>
    /// that matches with a component in the item model.
    /// </summary>
    protected override Control? BuildContentControl(CompositeItemModel<TKey> itemModel)
    {
        foreach (var template in AvailableTemplates)
        {
            if (!itemModel.TryGet(template.ComponentKey, template.ComponentType, out var component)) continue;

            // NOTE(erri120): DataTemplate.Build doesn't use the
            // data you give it, need to manually set the DataContext.
            // Otherwise. the new control will inherit the parent context,
            // which is CompositeItemModel<TKey>.
            var control = template.DataTemplate.Build(data: null);
            if (control is null) return null;

            control.DataContext = component;
            return control;
        }

        return Fallback;
    }

    /// <summary>
    /// Subscribes to component changes in the item model and rebuilds the content.
    /// </summary>
    protected override IDisposable Subscribe(CompositeItemModel<TKey> itemModel)
    {
        return itemModel.Components
            .ObserveChanged()
            .ObserveOnUIThreadDispatcher()
            .Subscribe((this, itemModel), static (_, state) =>
            {
                var (self, itemModel) = state;
                if (self.Presenter is null) return;

                var content = self.BuildContentControl(itemModel);
                self.Presenter.Content = content;
            });
    }
}
