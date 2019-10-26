using System;
using Microsoft.AspNetCore.Http.Features;

namespace MsgNThen.Adapter
{
    public class HttpRequestIdentifierFeature : IHttpRequestIdentifierFeature
    {
        private string _id;

        public string TraceIdentifier
        {
            get => this._id ?? (this._id = Guid.NewGuid().ToString());
            set => this._id = value;
        }
    }
}