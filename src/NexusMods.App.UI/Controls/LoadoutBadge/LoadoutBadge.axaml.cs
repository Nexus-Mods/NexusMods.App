using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.LoadoutBadge;

[PseudoClasses(":selected", ":applied", ":in-progress")]
public partial class LoadoutBadge : ReactiveUserControl<ILoadoutBadgeVM>
{
    public LoadoutBadge()
    {
        InitializeComponent();

        this.WhenActivated(d =>
            {
                this.WhenAnyValue(x => x.ViewModel!.IsLoadouotSelected)
                    .Subscribe(SetSelected)
                    .DisposeWith(d);

                this.WhenAnyValue(x => x.ViewModel!.IsLoadoutApplied)
                    .Subscribe(SetApplied)
                    .DisposeWith(d);

                this.WhenAnyValue(x => x.ViewModel!.IsLoadoutInProgress)
                    .Subscribe(SetInProgress)
                    .DisposeWith(d);
                
                this.OneWayBind(ViewModel, vm => vm.LoadoutShortName, v => v.ShortName.Text)
                    .DisposeWith(d);
            }
        );
    }
    
    private void SetSelected(bool selected)
    {
        PseudoClasses.Set(":selected", selected);
    }
    
    private void SetApplied(bool applied)
    {
        PseudoClasses.Set(":applied", applied);
    }
    
    private void SetInProgress(bool inProgress)
    {
        PseudoClasses.Set(":in-progress", inProgress);
    }
}

