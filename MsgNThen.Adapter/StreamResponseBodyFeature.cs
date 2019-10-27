using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

namespace MsgNThen.Adapter
{
    class StreamResponseBodyFeature : IHttpResponseBodyFeature
    {
        private PipeWriter _pipeWriter;
        private bool _started;
        private bool _completed;
        private bool _disposed;

        public StreamResponseBodyFeature(Stream stream)
        {
            Stream stream1 = stream;
            if (stream1 == null)
                throw new ArgumentNullException(nameof(stream));
            this.Stream = stream1;
        }

        public StreamResponseBodyFeature(Stream stream, IHttpResponseBodyFeature priorFeature)
        {
            Stream stream1 = stream;
            if (stream1 == null)
                throw new ArgumentNullException(nameof(stream));
            this.Stream = stream1;
            this.PriorFeature = priorFeature;
        }

        public Stream Stream { get; }

        public IHttpResponseBodyFeature PriorFeature { get; }

        public PipeWriter Writer
        {
            get
            {
                if (this._pipeWriter == null)
                {
                    this._pipeWriter = PipeWriter.Create(this.Stream, new StreamPipeWriterOptions((MemoryPool<byte>)null, -1, true));
                    if (this._completed)
                        this._pipeWriter.Complete((Exception)null);
                }
                return this._pipeWriter;
            }
        }

        public virtual void DisableBuffering()
        {
        }

        public virtual async Task SendFileAsync(
            string path,
            long offset,
            long? count,
            CancellationToken cancellationToken)
        {
            if (!this._started)
                await this.StartAsync(cancellationToken);
            await SendFileFallback.SendFileAsync(this.Stream, path, offset, count, cancellationToken);
        }

        public virtual Task StartAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (this._started)
                return Task.CompletedTask;
            this._started = true;
            return this.Stream.FlushAsync(cancellationToken);
        }

        public virtual async Task CompleteAsync()
        {
            if (this._disposed || this._completed)
                return;
            this._completed = true;
            if (this._pipeWriter == null)
                return;
            await this._pipeWriter.CompleteAsync((Exception)null);
        }

        public void Dispose()
        {
            this._disposed = true;
        }
    }
}