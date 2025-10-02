using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Resources;
using NexusMods.UI.Sdk;
using R3;

namespace NexusMods.App.UI.Controls;

/// <summary>
/// Collection of shared IItemModelComponents that can be used in TreeDataGrid.
/// </summary>
public static class SharedComponents
{
    public sealed class ViewModPageAction : ReactiveR3Object, IItemModelComponent<ViewModPageAction>, IComparable<ViewModPageAction>
    {
        public ReactiveCommand<Unit> CommandViewModPage { get; } = new();
        public IReadOnlyBindableReactiveProperty<bool> IsEnabled { get; }

        public int CompareTo(ViewModPageAction? other)
        {
            if (other is null) return 1;
            return 0; // All view mod page actions are considered equal for sorting
        }

        public ViewModPageAction(bool isEnabled = true)
        {
            IsEnabled = new BindableReactiveProperty<bool>(isEnabled);
        }

        private bool _isDisposed;
        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    Disposable.Dispose(CommandViewModPage, IsEnabled);
                }

                _isDisposed = true;
            }

            base.Dispose(disposing);
        }
    }
    
    public sealed class ViewModFilesAction : ReactiveR3Object, IItemModelComponent<ViewModFilesAction>, IComparable<ViewModFilesAction>
    {
        public ReactiveCommand<NavigationInformation, NavigationInformation> Command { get; } = new(info => info);
        public IReadOnlyBindableReactiveProperty<bool> IsEnabled { get; }

        public int CompareTo(ViewModFilesAction? other)
        {
            if (other is null) return 1;
            return 0; // All open file location actions are considered equal for sorting
        }

        public ViewModFilesAction(bool isEnabled = true)
        {
            IsEnabled = new BindableReactiveProperty<bool>(isEnabled);
        }

        private bool _isDisposed;
        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    Disposable.Dispose(Command, IsEnabled);
                }

                _isDisposed = true;
            }

            base.Dispose(disposing);
        }
    }
    
    public sealed class UninstallItemAction : ReactiveR3Object, IItemModelComponent<UninstallItemAction>, IComparable<UninstallItemAction>
    {
        public ReactiveCommand<Unit> CommandUninstallItem { get; } = new();
        public IReadOnlyBindableReactiveProperty<bool> IsEnabled { get; }

        public IReadOnlyBindableReactiveProperty<string> DisplayText { get;  }

        private string EnabledText => Language.Loadout_UninstallItem_Menu_Text;

        private string DisabledText => Language.Loadout_UninstallItem_Menu_Text__Uninstall_read_only;

        public int CompareTo(UninstallItemAction? other)
        {
            if (other is null) return 1;
            return 0; // All uninstall item actions are considered equal for sorting
        }

        public UninstallItemAction(bool isEnabled = true)
        {
            IsEnabled = new BindableReactiveProperty<bool>(isEnabled);
            DisplayText = new BindableReactiveProperty<string>(isEnabled ? EnabledText : DisabledText);
        }
        
        public UninstallItemAction(Observable<bool> isEnabled)
        {
            IsEnabled = isEnabled.ToBindableReactiveProperty();
            
            DisplayText = isEnabled
                .Select(enabled => enabled ? EnabledText : DisabledText)
                .ToBindableReactiveProperty(EnabledText);
        }

        private bool _isDisposed;
        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    Disposable.Dispose(CommandUninstallItem, IsEnabled);
                }

                _isDisposed = true;
            }

            base.Dispose(disposing);
        }
    }
}
