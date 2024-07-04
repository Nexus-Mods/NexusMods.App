using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.LoadoutBadge;

[PseudoClasses(":selected", ":in-progress", ":applied")]
public partial class LoadoutBadge : ReactiveUserControl<ILoadoutBadgeVM>
{
    public LoadoutBadge()
    {
        InitializeComponent();

        this.WhenActivated(d =>
            {
                this.WhenAnyValue(x => x.ViewModel!.IsLoadoutSelected)
                    .OnUI()
                    .Subscribe(SetSelected)
                    .DisposeWith(d);

                this.WhenAnyValue(x => x.ViewModel!.IsLoadoutApplied)
                    .OnUI()
                    .Subscribe(SetApplied)
                    .DisposeWith(d);

                this.WhenAnyValue(x => x.ViewModel!.IsLoadoutInProgress)
                    .OnUI()
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

