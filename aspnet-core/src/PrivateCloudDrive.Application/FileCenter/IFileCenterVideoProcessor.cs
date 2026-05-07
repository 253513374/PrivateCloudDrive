using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PrivateCloudDrive.FileCenter;

public interface IFileCenterVideoProcessor
{
    Task<FileCenterVideoProcessingResult> ProcessAsync(
        Stream videoStream,
        string fileName,
        CancellationToken cancellationToken = default);
}
