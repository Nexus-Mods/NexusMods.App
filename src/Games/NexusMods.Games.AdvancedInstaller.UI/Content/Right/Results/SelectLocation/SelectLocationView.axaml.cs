﻿using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;

[ExcludeFromCodeCoverage]
public partial class SelectLocationView : ReactiveUserControl<ISelectLocationViewModel>
{
    public SelectLocationView()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(view => view.ViewModel!.SuggestedEntries)
                .BindTo(this, view => view.SuggestedLocationItemsControl.ItemsSource)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.AllFoldersTrees,
                view => view.AllFoldersItemsControl.ItemsSource).DisposeWith(disposables);
        });
    }
}
