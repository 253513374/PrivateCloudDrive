using System.Collections.ObjectModel;
using PrivateCloudDrive.App.Localization;
using PrivateCloudDrive.App.Models;
using PrivateCloudDrive.App.Services;

namespace PrivateCloudDrive.App.Views;

/// <summary>
/// 表示TrashPage页面，承载移动端界面交互和页面级状态绑定。
/// </summary>
public partial class TrashPage : ContentPage
{
    private readonly ICloudDriveApiClient _apiClient = AppServices.GetRequiredService<ICloudDriveApiClient>();
    private bool _isSelectionMode;

    public ObservableCollection<CloudDriveItem> TrashItems { get; } = [];

    /// <summary>
    /// 初始化 <see cref="TrashPage"/> 的新实例，并注入完成业务处理所需的依赖。
    /// </summary>
    public TrashPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadTrashAsync();
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        await LoadTrashAsync();
    }

    private async void OnRestoreClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: CloudDriveItem item })
        {
            return;
        }

        try
        {
            await _apiClient.RestoreTrashItemAsync(item.Id);
            await LoadTrashAsync();
        }
        catch (Exception exception)
        {
            await ShowErrorAsync(
                AppText.Format(nameof(AppText.UnableToRestore), item.Name, exception.Message));
        }
    }

    private async void OnPermanentDeleteClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: CloudDriveItem item })
        {
            return;
        }

        var confirmed = await DisplayAlertAsync(
            AppText.DeleteForever,
            AppText.Format(nameof(AppText.PermanentlyDeleteQuestion), item.Name),
            AppText.Delete,
            AppText.Cancel);

        if (!confirmed)
        {
            return;
        }

        try
        {
            await _apiClient.PermanentlyDeleteTrashItemAsync(item.Id);
            await LoadTrashAsync();
        }
        catch (Exception exception)
        {
            await ShowErrorAsync(AppText.Format(nameof(AppText.UnableToPermanentlyDelete), item.Name, exception.Message));
        }
    }

    private async void OnEmptyTrashClicked(object? sender, EventArgs e)
    {
        if (TrashItems.Count == 0)
        {
            await ShowInfoAsync(AppText.TrashAlreadyEmpty);
            return;
        }

        var confirmed = await DisplayAlertAsync(
            AppText.EmptyTrash,
            AppText.EmptyTrashQuestion,
            AppText.Empty,
            AppText.Cancel);

        if (!confirmed)
        {
            return;
        }

        try
        {
            await _apiClient.EmptyTrashAsync();
            SetSelectionMode(false);
            await LoadTrashAsync();
        }
        catch (Exception exception)
        {
            await ShowErrorAsync(AppText.Format(nameof(AppText.UnableToEmptyTrash), exception.Message));
        }
    }

    private void OnToggleSelectionModeClicked(object? sender, EventArgs e)
    {
        SetSelectionMode(!_isSelectionMode);
    }

    private void OnTrashSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateSelectedItems();
    }

    private async void OnBatchRestoreClicked(object? sender, EventArgs e)
    {
        var selectedItems = GetSelectedItems();
        if (selectedItems.Count == 0)
        {
            await ShowInfoAsync("请先选择回收站项目。");
            return;
        }

        try
        {
            await _apiClient.RestoreTrashItemsAsync(selectedItems.Select(item => item.Id).ToList());
            SetSelectionMode(false);
            await LoadTrashAsync();
        }
        catch (Exception exception)
        {
            await ShowErrorAsync(exception.Message);
        }
    }

    private async void OnBatchPermanentDeleteClicked(object? sender, EventArgs e)
    {
        var selectedItems = GetSelectedItems();
        if (selectedItems.Count == 0)
        {
            await ShowInfoAsync("请先选择回收站项目。");
            return;
        }

        var confirmed = await DisplayAlertAsync(
            AppText.DeleteForever,
            $"永久删除 {selectedItems.Count} 项？此操作不可恢复。",
            AppText.Delete,
            AppText.Cancel);

        if (!confirmed)
        {
            return;
        }

        try
        {
            await _apiClient.PermanentlyDeleteTrashItemsAsync(selectedItems.Select(item => item.Id).ToList());
            SetSelectionMode(false);
            await LoadTrashAsync();
        }
        catch (Exception exception)
        {
            await ShowErrorAsync(exception.Message);
        }
    }

    private async Task LoadTrashAsync()
    {
        SetLoadingState(AppText.LoadingTrash);

        try
        {
            var items = await _apiClient.GetTrashItemsAsync();
            TrashItems.Clear();

            foreach (var item in items)
            {
                TrashItems.Add(item);
            }

            SetIdleState();
        }
        catch (Exception exception)
        {
            TrashItems.Clear();
            await ShowErrorAsync(exception.Message);
        }
    }

    private void SetLoadingState(string message)
    {
        StatePanel.IsVisible = true;
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        TrashRetryButton.IsVisible = false;
        StateLabel.Text = message;
    }

    private void SetIdleState()
    {
        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;
        TrashRetryButton.IsVisible = false;
        StatePanel.IsVisible = false;
    }

    private Task ShowInfoAsync(string message)
    {
        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;
        TrashRetryButton.IsVisible = false;
        StatePanel.IsVisible = true;
        StateLabel.Text = message;

        return Task.CompletedTask;
    }

    private Task ShowErrorAsync(string message)
    {
        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;
        TrashRetryButton.IsVisible = true;
        StatePanel.IsVisible = true;
        StateLabel.Text = message;

        return Task.CompletedTask;
    }

    private void SetSelectionMode(bool isSelectionMode)
    {
        _isSelectionMode = isSelectionMode;
        TrashSelectionModeButton.Text = _isSelectionMode ? "完成" : "选择";
        TrashBatchToolbar.IsVisible = _isSelectionMode;
        TrashCollectionView.SelectionMode = _isSelectionMode
            ? SelectionMode.Multiple
            : SelectionMode.None;

        TrashCollectionView.SelectedItems.Clear();
        UpdateSelectedItems();
    }

    private IReadOnlyList<CloudDriveItem> GetSelectedItems()
    {
        return TrashCollectionView.SelectedItems
            .OfType<CloudDriveItem>()
            .ToList();
    }

    private void UpdateSelectedItems()
    {
        TrashSelectedCountLabel.Text = $"已选择 {GetSelectedItems().Count} 项";
    }
}
