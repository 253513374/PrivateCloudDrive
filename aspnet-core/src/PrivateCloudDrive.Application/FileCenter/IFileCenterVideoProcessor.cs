using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PrivateCloudDrive.FileCenter;

/// <summary>
/// 表示文件中心IFileCenterVideoProcessor，参与私有云盘文件、目录、分享、标签或媒体处理流程。
/// </summary>
public interface IFileCenterVideoProcessor
{
    Task<FileCenterVideoProcessingResult> ProcessAsync(
        Stream videoStream,
        string fileName,
        CancellationToken cancellationToken = default);
}
