using System;
using Microsoft.AspNetCore.Http.Features;

namespace MsgNThen.Adapter
{
    public class ServiceProvidersFeature : IServiceProvidersFeature
    {
        public IServiceProvider RequestServices { get; set; }
    }
}