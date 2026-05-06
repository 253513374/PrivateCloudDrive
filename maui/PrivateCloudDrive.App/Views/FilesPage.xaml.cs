using System.Collections.ObjectModel;
using PrivateCloudDrive.App.Models;
using PrivateCloudDrive.App.Services;

namespace PrivateCloudDrive.App.Views;

public partial class FilesPage : ContentPage
{
    private readonly MockCloudDriveApiClient _apiClient = new();

    public ObservableCollection<CloudDriveItem> Items { get; } = [];

    public string ApiBaseUrl => AppSettings.ApiBaseUrl;

    public FilesPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadItemsAsync();
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        await LoadItemsAsync();
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        await _apiClient.SignOutAsync();
        await Shell.Current.GoToAsync("//login", true);
    }

    private async Task LoadItemsAsync()
    {
        RefreshButton.IsEnabled = false;

        try
        {
            var items = await _apiClient.GetRootItemsAsync();
            Items.Clear();

            foreach (var item in items)
            {
                Items.Add(item);
            }
        }
        finally
        {
            RefreshButton.IsEnabled = true;
        }
    }
}
