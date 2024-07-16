using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.LoadoutCard;

public partial class CreateNewLoadoutCardView : ReactiveUserControl<ICreateNewLoadoutCardViewModel>
{
    public CreateNewLoadoutCardView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.BindCommand(ViewModel, vm => vm.AddLoadoutCommand, v => v.CreateNewLoadoutButton)
                .DisposeWith(d);
        });
    }
}

