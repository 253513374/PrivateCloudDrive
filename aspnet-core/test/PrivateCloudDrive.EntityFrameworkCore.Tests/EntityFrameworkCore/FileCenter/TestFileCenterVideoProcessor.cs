using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PrivateCloudDrive.FileCenter;

namespace PrivateCloudDrive.EntityFrameworkCore.FileCenter;

public class TestFileCenterVideoProcessor : IFileCenterVideoProcessor
{
    public static readonly byte[] ThumbnailBytes = Encoding.UTF8.GetBytes("test-video-cover");

    public Task<FileCenterVideoProcessingResult> ProcessAsync(
        Stream videoStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new FileCenterVideoProcessingResult
        {
            Width = 640,
            Height = 360,
            DurationMilliseconds = 123456,
            Codec = "h264",
            ThumbnailBytes = ThumbnailBytes,
            MetadataJson = JsonSerializer.Serialize(new
            {
                Width = 640,
                Height = 360,
                DurationMilliseconds = 123456,
                Codec = "h264"
            })
        });
    }
}
