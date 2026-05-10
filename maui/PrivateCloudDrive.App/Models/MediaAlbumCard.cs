using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PrivateCloudDrive.App.Models;

public sealed class MediaAlbumCard : INotifyPropertyChanged
{
    private ImageSource? _coverSource;

    public MediaAlbumCard(MediaAlbum album)
    {
        Album = album;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MediaAlbum Album { get; }

    public Guid Id => Album.Id;

    public string Name => Album.Name;

    public string? Description => Album.Description;

    public Guid? CoverFileNodeId => Album.CoverFileNodeId;

    public string ItemsCountText => $"{Album.ItemsCount} 项";

    public string UpdatedText
    {
        get
        {
            var time = Album.LastModificationTime ?? Album.CreationTime;
            return $"更新于 {time:yyyy/M/d}";
        }
    }

    public string SummaryText => string.IsNullOrWhiteSpace(Description)
        ? $"{ItemsCountText} · {UpdatedText}"
        : $"{ItemsCountText} · {Description}";

    public ImageSource? CoverSource
    {
        get => _coverSource;
        set
        {
            if (_coverSource == value)
            {
                return;
            }

            _coverSource = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasCover));
            OnPropertyChanged(nameof(ShowPlaceholder));
        }
    }

    public bool HasCover => CoverSource != null;

    public bool ShowPlaceholder => CoverSource == null;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
