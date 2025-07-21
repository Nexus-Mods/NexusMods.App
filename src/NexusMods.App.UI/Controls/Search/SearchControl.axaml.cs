using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using JetBrains.Annotations;
using R3;
using ReactiveUI;
using static NexusMods.App.UI.Controls.Filters.Filter;
using CompositeDisposable = System.Reactive.Disposables.CompositeDisposable;
using Disposable = System.Reactive.Disposables.Disposable;

namespace NexusMods.App.UI.Controls.Search;

[UsedImplicitly]
public partial class SearchControl : UserControl
{
    /// <summary>
    /// The adapter that supports search functionality.
    /// </summary>
    public static readonly StyledProperty<ISearchableAdapter?> AdapterProperty =
        AvaloniaProperty.Register<SearchControl, ISearchableAdapter?>(nameof(Adapter));

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

    public ISearchableAdapter? Adapter
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
        // Setup search text binding
        this.WhenAnyValue(x => x.SearchTextBox.Text)
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
                    Adapter?.OnOpenSearchPanel(PageName);
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

        // Add disposal logic to auto remove the event handler
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
