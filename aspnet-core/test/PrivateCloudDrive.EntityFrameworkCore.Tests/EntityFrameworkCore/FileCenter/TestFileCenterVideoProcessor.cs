using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PrivateCloudDrive.FileCenter;

namespace PrivateCloudDrive.EntityFrameworkCore.FileCenter;

/// <summary>
/// 表示文件中心TestFileCenterVideoProcessor，参与私有云盘文件、目录、分享、标签或媒体处理流程。
/// </summary>
public class TestFileCenterVideoProcessor : IFileCenterVideoProcessor
{
    public static readonly byte[] ThumbnailBytes = Encoding.UTF8.GetBytes("test-video-cover");

    /// <summary>
    /// 处理异步或耗时业务任务，并产出后续流程所需的结果。
    /// </summary>
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
