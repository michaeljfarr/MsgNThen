using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MsgNThen.Adapter
{
    internal static class StreamCopyOperationInternal
    {
        private const int DefaultBufferSize = 4096;

        public static async Task CopyToAsync(
            Stream source,
            Stream destination,
            long? count,
            int bufferSize,
            CancellationToken cancel)
        {
            long? bytesRemaining = count;
            byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            try
            {
                while (!bytesRemaining.HasValue || bytesRemaining.GetValueOrDefault() > 0L)
                {
                    cancel.ThrowIfCancellationRequested();
                    int count1 = buffer.Length;
                    if (bytesRemaining.HasValue)
                        count1 = (int)Math.Min(bytesRemaining.GetValueOrDefault(), (long)count1);
                    int count2 = await source.ReadAsync(buffer, 0, count1, cancel);
                    if (bytesRemaining.HasValue)
                    {
                        long? nullable = bytesRemaining;
                        long num = (long)count2;
                        bytesRemaining = nullable.HasValue ? new long?(nullable.GetValueOrDefault() - num) : new long?();
                    }
                    if (count2 == 0)
                        break;
                    cancel.ThrowIfCancellationRequested();
                    await destination.WriteAsync(buffer, 0, count2, cancel);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer, false);
            }
        }
    }
}