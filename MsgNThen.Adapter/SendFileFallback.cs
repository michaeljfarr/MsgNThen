using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MsgNThen.Adapter
{
    public static class SendFileFallback
    {
        public static async Task SendFileAsync(
            Stream destination,
            string filePath,
            long offset,
            long? count,
            CancellationToken cancellationToken)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            if (offset < 0L || offset > fileInfo.Length)
                throw new ArgumentOutOfRangeException(nameof(offset), (object)offset, string.Empty);
            if (count.HasValue && (count.Value < 0L || count.Value > fileInfo.Length - offset))
                throw new ArgumentOutOfRangeException(nameof(count), (object)count, string.Empty);
            cancellationToken.ThrowIfCancellationRequested();
            int bufferSize = 16384;
            FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
            using (fileStream)
            {
                fileStream.Seek(offset, SeekOrigin.Begin);
                await StreamCopyOperationInternal.CopyToAsync((Stream)fileStream, destination, count, bufferSize, cancellationToken);
            }
        }
    }
}