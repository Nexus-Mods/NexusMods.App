﻿using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.DownloadGrid.Columns.DownloadSize;

public partial class DownloadSizeView : ReactiveUserControl<IDownloadSizeViewModel>
{
    public DownloadSizeView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(vm => vm.ViewModel!.Size)
                .BindToView(this, view => view.SizeTextBox.Text)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.DataContext)
                .SubscribeWithErrorLogging(logger: default)
                .DisposeWith(d);
        });
    }
}

