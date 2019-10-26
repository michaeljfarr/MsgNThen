using System.Security.Claims;
using Microsoft.AspNetCore.Http.Features.Authentication;

namespace MsgNThen.Adapter
{
    public class HttpAuthenticationFeature : IHttpAuthenticationFeature
    {
        public ClaimsPrincipal User { get; set; }

        public IAuthenticationHandler Handler { get; set; }
    }
}