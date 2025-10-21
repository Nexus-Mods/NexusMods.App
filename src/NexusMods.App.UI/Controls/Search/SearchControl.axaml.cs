using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using AvaloniaEdit.Search;
using JetBrains.Annotations;
using NexusMods.Telemetry;
using R3;
using ReactiveUI;
using static NexusMods.App.UI.Controls.Filters.Filter;
using CompositeDisposable = System.Reactive.Disposables.CompositeDisposable;
using Disposable = System.Reactive.Disposables.Disposable;

namespace NexusMods.App.UI.Controls.Search;

/// <remarks>
/// Note(sewer):
/// This represents a standalone control which implements a search button, search box
/// and a clear button.
///
/// Since this is not a full view, or a component you would arbitrarily render,
/// we are not using <see cref="ReactiveUserControl{TViewModel}"/> here. Same way you wouldn't
/// make a <see cref="ReactiveUserControl"/>  for a Button or a TextBox.
/// </remarks>
[UsedImplicitly]
public partial class SearchControl : UserControl
{
    /// <summary>
    /// The <see cref="TreeDataGridAdapter{TModel,TKey}"/> that supports search functionality.
    /// </summary>
    public static readonly StyledProperty<ISearchableTreeDataGridAdapter?> AdapterProperty =
        AvaloniaProperty.Register<SearchControl, ISearchableTreeDataGridAdapter?>(nameof(Adapter));

    /// <summary>
    /// The name of the page for telemetry tracking.
    /// </summary>
    public static readonly StyledProperty<string> PageNameProperty =
        AvaloniaProperty.Register<SearchControl, string>(nameof(PageName), defaultValue: "Unknown");

    /// <summary>
    /// The size of the search button.
    /// </summary>
    public static readonly StyledProperty<StandardButton.Sizes> ButtonSizeProperty =
        AvaloniaProperty.Register<SearchControl, StandardButton.Sizes>(nameof(ButtonSize), defaultValue: StandardButton.Sizes.Toolbar);

    public ISearchableTreeDataGridAdapter? Adapter
    {
        get => GetValue(AdapterProperty);
        set => SetValue(AdapterProperty, value);
    }

    public string PageName
    {
        get => GetValue(PageNameProperty);
        set => SetValue(PageNameProperty, value);
    }

    public StandardButton.Sizes ButtonSize
    {
        get => GetValue(ButtonSizeProperty);
        set => SetValue(ButtonSizeProperty, value);
    }

    /// <summary>
    /// Gets whether the search panel is currently visible.
    /// </summary>
    public bool IsSearchVisible => SearchPanel.IsVisible;

    /// <summary>
    /// Gets the current search text.
    /// </summary>
    [PublicAPI]
    public string SearchText => SearchTextBox.Text ?? string.Empty;

    public SearchControl()
    {
        InitializeComponent();
        SetupBindings();
    }

    private void SetupBindings()
    {
        // Note(sewer): The SearchControl subscribes to self here.
        // So there is no risk of event leaks, as the lifetime of self equals self.

        // Setup search text binding
        this.WhenAnyValue(x => x.SearchTextBox.Text)
            .Throttle(dueTime: TimeSpan.FromMilliseconds(100))
            .OnUI()
            .Subscribe(ApplySearchFilter);

        // Clear button functionality
        SearchClearButton.Click += (_, _) => ClearSearch();

        // Handle focus when search panel visibility changes
        this.WhenAnyValue(x => x.SearchPanel.IsVisible)
            .Skip(1) // Skip the initial value to avoid focusing on startup
            .OnUI()
            .Subscribe(isVisible =>
            {
                if (isVisible)
                {
                    // Focus the textbox when the search panel becomes visible
                    SearchTextBox.Focus();

                    // Tracking
                    Tracking.AddEvent(Events.Search.OpenSearch, new EventMetadata(name: PageName));
                }
            });
    }

    private void ApplySearchFilter(string? searchText)
    {
        if (Adapter?.Filter != null)
        {
            Adapter.Filter.Value = string.IsNullOrWhiteSpace(searchText)
                ? NoFilter.Instance
                : new TextFilter(searchText, CaseSensitive: false);
        }
    }

    private void SearchButton_OnClick(object? sender, RoutedEventArgs e) => ToggleSearchPanelVisibility();

    /// <summary>
    /// Toggles the visibility of the search panel.
    /// </summary>
    public void ToggleSearchPanelVisibility() => SearchPanel.IsVisible = !SearchPanel.IsVisible;

    /// <summary>
    /// Clears the search text and hides the search panel.
    /// </summary>
    public void ClearSearch()
    {
        SearchTextBox.Text = string.Empty;
        SearchPanel.IsVisible = false;
    }

    /// <summary>
    /// Attaches keyboard shortcuts (Ctrl+F, Escape) to the specified control within a WhenActivated block.
    /// This ensures proper disposal when the view is deactivated.
    /// </summary>
    /// <param name="control">The control to attach keyboard handlers to</param>
    /// <param name="disposables">The disposables collection from WhenActivated</param>
    public void AttachKeyboardHandlers(Control control, CompositeDisposable disposables)
    {
        control.KeyDown += OnKeyDown;

        // Auto remove the event handler
        Disposable.Create(control, ctrl => ctrl.KeyDown -= OnKeyDown)
            .AddTo(disposables);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            // Handle Ctrl+F to toggle search panel
            case Key.F when e.KeyModifiers.HasFlag(KeyModifiers.Control):
                ToggleSearchPanelVisibility();
                e.Handled = true; // Prevent the event from bubbling up
                return;
            case Key.Escape when IsSearchVisible:
                ClearSearch();
                e.Handled = true; // Prevent the event from bubbling up
                return;
        }

    }
}
