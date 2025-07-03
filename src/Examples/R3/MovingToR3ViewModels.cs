using NexusMods.Abstractions.UI;
using R3;
using ReactiveUI.Fody.Helpers;

namespace Examples.R3;

// Moving from `[Reactive]` to R3:
file interface IOldViewModel : IViewModelInterface
{
    string ReadOnlyText { get; }
    string MutableText { get; set; }
}

file class OldViewModel : AViewModel<IOldViewModel>, IOldViewModel
{
    [Reactive] public string ReadOnlyText { get; private set; } = "Foo";
    [Reactive] public string MutableText { get; set; } = "Bar";
}

file interface INewViewModel : IViewModelInterface
{
    // Use the appropriate type for the property on whether the value can be changed from outside the View Model

    // IReadOnlyBindableReactiveProperty for properties that you bind one-way
    IReadOnlyBindableReactiveProperty<string> ReadOnlyText { get; }

    // IBindableReactiveProperty for properties that you bind two-way
    IBindableReactiveProperty<string> MutableText { get; }
}

file class NewViewModel : AViewModel<INewViewModel>, INewViewModel
{
    // Use explicit interface implementations. This allows you to implement the interface without
    // issue while having a property with the same name but different type.
    // Alternatively, use a private readonly field.

    public BindableReactiveProperty<string> ReadOnlyText { get; } = new();
    IReadOnlyBindableReactiveProperty<string> INewViewModel.ReadOnlyText => ReadOnlyText;

    public BindableReactiveProperty<string> MutableText { get; } = new();
    IBindableReactiveProperty<string> INewViewModel.MutableText => MutableText;
}
