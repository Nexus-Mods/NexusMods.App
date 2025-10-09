using Avalonia.Controls;
using NexusMods.UI.Sdk;
using R3;
using ReactiveUI;

namespace Examples.R3;

file interface IMyViewModel : IViewModelInterface
{
    IReadOnlyBindableReactiveProperty<int> Count { get; }

    IBindableReactiveProperty<string?> InputText { get; }
}

file class MyViewModel : AViewModel<IMyViewModel>, IMyViewModel
{
    public BindableReactiveProperty<int> Count { get; } = new();
    IReadOnlyBindableReactiveProperty<int> IMyViewModel.Count => Count;

    public BindableReactiveProperty<string?> InputText { get; } = new();
    IBindableReactiveProperty<string?> IMyViewModel.InputText => InputText;
}

file class MyView : R3UserControl<IMyViewModel>
{
    public TextBlock TextBlock { get; } = new();
    public TextBox TextBox { get; } = new();

    public MyView()
    {
        this.WhenActivated(disposables =>
        {
            // These methods look similar to their ReactiveUI counterparts but are much safer and faster.
            // Key differences are the lack of reflection and the strict typing, you have to specify how values are converted.
            this.OneWayR3Bind(static view => view.BindableViewModel, static vm => vm.Count, static (view, count) => view.TextBlock.Text = count.ToString())
                .AddTo(disposables);

            this.TwoWayR3Bind(static view => view.BindableViewModel, static vm => vm.InputText, static view => view.TextBox.Text, static (view, text) => view.TextBox.Text = text)
                .AddTo(disposables);
        });
    }
}
