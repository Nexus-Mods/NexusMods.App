using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.Templates;
using DynamicData.Kernel;
using JetBrains.Annotations;
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
        get => _dataTemplate ?? throw new InvalidOperationException($"Data template hasn't been set yet");
        set
        {
            if (value.DataType != ComponentType) throw new InvalidOperationException($"Mismatch between types, expected `{ComponentType}` got `{value.DataType}`");
            _dataTemplate = value;
        }
    }
}

/// <summary>
/// Custom <see cref="ContentControl"/> to reactively build the control.
/// </summary>
[PublicAPI]
public abstract class AReactiveContentControl<TContent> : ContentControl
    where TContent : class
{
    private readonly SerialDisposable _serialDisposable = new();

    /// <summary>
    /// Builds a control for the given content.
    /// </summary>
    protected abstract Control? BuildContentControl(TContent content, out Optional<string> contentPresenterClass);

    /// <summary>
    /// Gets an observable stream to re-trigger content changes.
    /// </summary>
    protected abstract Observable<Unit> GetObservable(TContent content);

    /// <summary>
    /// Subscribes to content changes.
    /// </summary>
    protected virtual IDisposable Subscribe(TContent content)
    {
        return GetObservable(content).ObserveOnUIThreadDispatcher().Subscribe((this, content), static (_, state) =>
        {
            var (self, content) = state;
            if (self.Presenter is null) return;

            var control = self.BuildContentControl(content, out var optionalClass);
            self.SetContentControl(control, optionalClass);
        });
    }

    protected override Type StyleKeyOverride => typeof(ContentControl);

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
            var control = BuildContentControl(content, out var optionalClass);
            SetContentControl(control, optionalClass);
        }

        return didRegister;
    }

    protected void SetContentControl(Control? contentControl, Optional<string> newClass)
    {
        if (Presenter is null) throw new InvalidOperationException();

        // NOTE(erri120): somewhat of a hack but this allows styles selector to work properly
        // otherwise there would be no parent to select. We can't select ContentPresenter
        // from styles because that's part of the template, and we can't use /template/
        // because the parent is generic, and generics don't work in style selectors...
        var border = new Border();
        if (newClass.HasValue) border.Classes.Set(newClass.Value, true);
        border.Child = contentControl;

        Presenter.Content = border;
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

[PublicAPI]
public class ComponentControl<TKey> : AReactiveContentControl<CompositeItemModel<TKey>>
    where TKey : notnull
{
    public IComponentTemplate? ComponentTemplate { get; set; }

    public Control? Fallback { get; set; }

    protected override Control? BuildContentControl(CompositeItemModel<TKey> itemModel, out Optional<string> contentPresenterClass)
    {
        contentPresenterClass = Optional<string>.None;

        if (ComponentTemplate is null) throw new InvalidOperationException();
        if (!itemModel.TryGet(ComponentTemplate.ComponentKey, ComponentTemplate.ComponentType, out var component)) return Fallback;

        // NOTE(erri120): DataTemplate.Build doesn't use the
        // data you give it, need to manually set the DataContext.
        // Otherwise. the new control will inherit the parent context,
        // which is CompositeItemModel<TKey>.
        var control = ComponentTemplate.DataTemplate.Build(data: null);
        if (control is null) return Fallback;

        control.DataContext = component;
        contentPresenterClass = ComponentTemplate.ComponentKey.Value;
        return control;
    }

    protected override Observable<Unit> GetObservable(CompositeItemModel<TKey> itemModel)
    {
        if (ComponentTemplate is null) throw new InvalidOperationException();
        var key = ComponentTemplate.ComponentKey;

        return itemModel.Components
            .ObserveKeyChanges(key)
            .Select(static _ => Unit.Default);
    }
}

/// <summary>
/// Control for columns where row models are <see cref="CompositeItemModel{TKey}"/>.
/// </summary>
[PublicAPI]
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
    protected override Control? BuildContentControl(CompositeItemModel<TKey> itemModel, out Optional<string> contentPresenterClass)
    {
        contentPresenterClass = Optional<string>.None;

        foreach (var template in AvailableTemplates)
        {
            if (!itemModel.TryGet(template.ComponentKey, template.ComponentType, out var component)) continue;

            // NOTE(erri120): DataTemplate.Build doesn't use the
            // data you give it, need to manually set the DataContext.
            // Otherwise. the new control will inherit the parent context,
            // which is CompositeItemModel<TKey>.
            var control = template.DataTemplate.Build(data: null);
            if (control is null) continue;

            control.DataContext = component;
            contentPresenterClass = template.ComponentKey.Value;
            return control;
        }

        return Fallback;
    }

    protected override Observable<Unit> GetObservable(CompositeItemModel<TKey> itemModel)
    {
        return itemModel.Components
            .ObserveChanged()
            .Select(static _ => Unit.Default);
    }
}
