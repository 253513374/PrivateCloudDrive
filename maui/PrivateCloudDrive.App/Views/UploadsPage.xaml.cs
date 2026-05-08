using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Specialized;
using PrivateCloudDrive.App.Localization;
using PrivateCloudDrive.App.Models;
using PrivateCloudDrive.App.Services;

namespace PrivateCloudDrive.App.Views;

public partial class UploadsPage : ContentPage
{
    private readonly IUploadQueueService _uploadQueueService = AppServices.GetRequiredService<IUploadQueueService>();

    public ObservableCollection<UploadQueueItem> UploadItems => _uploadQueueService.Items;

    public UploadsPage()
    {
        InitializeComponent();
        BindingContext = this;
        UploadItems.CollectionChanged += OnUploadItemsChanged;
        foreach (var item in UploadItems)
        {
            item.PropertyChanged += OnUploadItemPropertyChanged;
        }

        UpdateQueueState();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateQueueState();
    }

    private void OnClearCompletedClicked(object? sender, EventArgs e)
    {
        _uploadQueueService.ClearCompleted();
        UpdateQueueState();
    }

    private void OnUploadItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (UploadQueueItem item in e.OldItems)
            {
                item.PropertyChanged -= OnUploadItemPropertyChanged;
            }
        }

        if (e.NewItems != null)
        {
            foreach (UploadQueueItem item in e.NewItems)
            {
                item.PropertyChanged += OnUploadItemPropertyChanged;
            }
        }

        UpdateQueueState();
    }

    private void OnUploadItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UploadQueueItem.Status))
        {
            UpdateQueueState();
        }
    }

    private void UpdateQueueState()
    {
        var waiting = UploadItems.Count(item => item.Status == UploadQueueStatus.Waiting);
        var uploading = UploadItems.Count(item => item.Status == UploadQueueStatus.Uploading);
        var failed = UploadItems.Count(item => item.Status == UploadQueueStatus.Failed);
        var completed = UploadItems.Count(item => item.Status == UploadQueueStatus.Completed);

        QueueStateLabel.Text = UploadItems.Count == 0
            ? AppText.UploadQueueEmpty
            : AppText.Format(nameof(AppText.UploadQueueSummary), uploading, waiting, failed, completed);
    }
}
