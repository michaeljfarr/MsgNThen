namespace MsgNThen.Adapter
{
    public class FormOptions
    {
        public const int DefaultMemoryBufferThreshold = 65536;
        public const int DefaultBufferBodyLengthLimit = 134217728;
        public const int DefaultMultipartBoundaryLengthLimit = 128;
        public const long DefaultMultipartBodyLengthLimit = 134217728;

        /// <summary>
        /// Enables full request body buffering. Use this if multiple components need to read the raw stream.
        /// The default value is false.
        /// </summary>
        public bool BufferBody { get; set; }

        /// <summary>
        /// If <see cref="P:Microsoft.AspNetCore.Http.Features.FormOptions.BufferBody" /> is enabled, this many bytes of the body will be buffered in memory.
        /// If this threshold is exceeded then the buffer will be moved to a temp file on disk instead.
        /// This also applies when buffering individual multipart section bodies.
        /// </summary>
        public int MemoryBufferThreshold { get; set; } = 65536;

        /// <summary>
        /// If <see cref="P:Microsoft.AspNetCore.Http.Features.FormOptions.BufferBody" /> is enabled, this is the limit for the total number of bytes that will
        /// be buffered. Forms that exceed this limit will throw an <see cref="T:System.IO.InvalidDataException" /> when parsed.
        /// </summary>
        public long BufferBodyLengthLimit { get; set; } = 134217728;

        /// <summary>
        /// A limit for the number of form entries to allow.
        /// Forms that exceed this limit will throw an <see cref="T:System.IO.InvalidDataException" /> when parsed.
        /// </summary>
        public int ValueCountLimit { get; set; } = 1024;

        /// <summary>
        /// A limit on the length of individual keys. Forms containing keys that exceed this limit will
        /// throw an <see cref="T:System.IO.InvalidDataException" /> when parsed.
        /// </summary>
        public int KeyLengthLimit { get; set; } = 2048;

        /// <summary>
        /// A limit on the length of individual form values. Forms containing values that exceed this
        /// limit will throw an <see cref="T:System.IO.InvalidDataException" /> when parsed.
        /// </summary>
        public int ValueLengthLimit { get; set; } = 4194304;

        /// <summary>
        /// A limit for the length of the boundary identifier. Forms with boundaries that exceed this
        /// limit will throw an <see cref="T:System.IO.InvalidDataException" /> when parsed.
        /// </summary>
        public int MultipartBoundaryLengthLimit { get; set; } = 128;

        /// <summary>
        /// A limit for the number of headers to allow in each multipart section. Headers with the same name will
        /// be combined. Form sections that exceed this limit will throw an <see cref="T:System.IO.InvalidDataException" />
        /// when parsed.
        /// </summary>
        public int MultipartHeadersCountLimit { get; set; } = 16;

        /// <summary>
        /// A limit for the total length of the header keys and values in each multipart section.
        /// Form sections that exceed this limit will throw an <see cref="T:System.IO.InvalidDataException" /> when parsed.
        /// </summary>
        public int MultipartHeadersLengthLimit { get; set; } = 16384;

        /// <summary>
        /// A limit for the length of each multipart body. Forms sections that exceed this limit will throw an
        /// <see cref="T:System.IO.InvalidDataException" /> when parsed.
        /// </summary>
        public long MultipartBodyLengthLimit { get; set; } = 134217728;
    }
}