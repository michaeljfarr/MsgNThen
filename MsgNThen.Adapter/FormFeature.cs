using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace MsgNThen.Adapter
{
    public class FormFeature : IFormFeature
    {
        private static readonly FormOptions DefaultFormOptions = new FormOptions();
        private readonly HttpRequest _request;
        private readonly FormOptions _options;
        private Task<IFormCollection> _parsedFormTask;
        private IFormCollection _form;

        public FormFeature(IFormCollection form)
        {
            if (form == null)
                throw new ArgumentNullException(nameof(form));
            this.Form = form;
        }

        public FormFeature(HttpRequest request)
            : this(request, FormFeature.DefaultFormOptions)
        {
        }

        public FormFeature(HttpRequest request, FormOptions options)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            this._request = request;
            this._options = options;
        }

        private MediaTypeHeaderValue ContentType
        {
            get
            {
                MediaTypeHeaderValue parsedValue;
                MediaTypeHeaderValue.TryParse((StringSegment)this._request.ContentType, out parsedValue);
                return parsedValue;
            }
        }

        public bool HasFormContentType
        {
            get
            {
                if (this.Form != null)
                    return true;
                MediaTypeHeaderValue contentType = this.ContentType;
                if (!this.HasApplicationFormContentType(contentType))
                    return this.HasMultipartFormContentType(contentType);
                return true;
            }
        }

        public IFormCollection Form
        {
            get
            {
                return this._form;
            }
            set
            {
                this._parsedFormTask = (Task<IFormCollection>)null;
                this._form = value;
            }
        }

        public IFormCollection ReadForm()
        {
            if (this.Form != null)
                return this.Form;
            if (!this.HasFormContentType)
                throw new InvalidOperationException("Incorrect Content-Type: " + this._request.ContentType);
            return this.ReadFormAsync().GetAwaiter().GetResult();
        }

        public Task<IFormCollection> ReadFormAsync()
        {
            return this.ReadFormAsync(CancellationToken.None);
        }

        public Task<IFormCollection> ReadFormAsync(
            CancellationToken cancellationToken)
        {
            if (this._parsedFormTask == null)
                this._parsedFormTask = this.Form == null ? this.InnerReadFormAsync(cancellationToken) : Task.FromResult<IFormCollection>(this.Form);
            return this._parsedFormTask;
        }

        private async Task<IFormCollection> InnerReadFormAsync(
            CancellationToken cancellationToken)
        {
            if (!this.HasFormContentType)
                throw new InvalidOperationException("Incorrect Content-Type: " + this._request.ContentType);
            
            this.Form = (IFormCollection)FormCollection.Empty;
            return this.Form;
        }

        private Encoding FilterEncoding(Encoding encoding)
        {
            if (encoding == null || Encoding.UTF7.Equals((object)encoding))
                return Encoding.UTF8;
            return encoding;
        }

        private bool HasApplicationFormContentType(MediaTypeHeaderValue contentType)
        {
            if (contentType != null)
                return contentType.MediaType.Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase);
            return false;
        }

        private bool HasMultipartFormContentType(MediaTypeHeaderValue contentType)
        {
            if (contentType != null)
                return contentType.MediaType.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase);
            return false;
        }

        private bool HasFormDataContentDisposition(ContentDispositionHeaderValue contentDisposition)
        {
            if (contentDisposition != null && (contentDisposition.DispositionType.Equals("form-data") && StringSegment.IsNullOrEmpty(contentDisposition.FileName)))
                return StringSegment.IsNullOrEmpty(contentDisposition.FileNameStar);
            return false;
        }

        private bool HasFileContentDisposition(ContentDispositionHeaderValue contentDisposition)
        {
            if (contentDisposition == null || !contentDisposition.DispositionType.Equals("form-data"))
                return false;
            if (StringSegment.IsNullOrEmpty(contentDisposition.FileName))
                return !StringSegment.IsNullOrEmpty(contentDisposition.FileNameStar);
            return true;
        }

        private static string GetBoundary(MediaTypeHeaderValue contentType, int lengthLimit)
        {
            StringSegment stringSegment = HeaderUtilities.RemoveQuotes(contentType.Boundary);
            if (StringSegment.IsNullOrEmpty(stringSegment))
                throw new InvalidDataException("Missing content-type boundary.");
            if (stringSegment.Length > lengthLimit)
                throw new InvalidDataException(string.Format("Multipart boundary length limit {0} exceeded.", (object)lengthLimit));
            return stringSegment.ToString();
        }
    }
}