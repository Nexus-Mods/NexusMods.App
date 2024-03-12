using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.ModInfo;

public partial class ModInfoView : ReactiveUserControl<IModInfoViewModel>
{
    public ModInfoView()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            this.OneWayBind(ViewModel, vm => vm.SectionViewModel, view => view.ModInfoCategoryViewHost.ViewModel)
                .DisposeWith(d);
        });
    }
}

