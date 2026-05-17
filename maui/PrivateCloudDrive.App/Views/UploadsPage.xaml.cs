using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Specialized;
using PrivateCloudDrive.App.Localization;
using PrivateCloudDrive.App.Models;
using PrivateCloudDrive.App.Services;

namespace PrivateCloudDrive.App.Views;

/// <summary>
/// 表示UploadsPage页面，承载移动端界面交互和页面级状态绑定。
/// </summary>
public partial class UploadsPage : ContentPage
{
    private readonly IUploadQueueService _uploadQueueService = AppServices.GetRequiredService<IUploadQueueService>();
    private readonly IBackupTransferService _backupTransferService = AppServices.GetRequiredService<IBackupTransferService>();

    public ObservableCollection<UploadQueueItem> UploadItems => _uploadQueueService.Items;

    /// <summary>
    /// 初始化 <see cref="UploadsPage"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
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

    private async void OnGoToFilesClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//main/files", true);
    }

    private async void OnRetryBackupClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not UploadQueueItem item)
        {
            return;
        }

        await _backupTransferService.RetryAsync(item);
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

        QueueStatePanel.IsVisible = UploadItems.Count > 0;
        ClearCompletedButton.IsVisible = completed > 0;

        QueueStateLabel.Text = UploadItems.Count == 0
            ? string.Empty
            : AppText.Format(nameof(AppText.UploadQueueSummary), uploading, waiting, failed, completed);
    }
}
