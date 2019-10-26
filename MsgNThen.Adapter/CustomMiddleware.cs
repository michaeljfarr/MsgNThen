using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MsgNThen.Adapter
{
    public class CustomMiddleware
    {
        private readonly RequestDelegate _next;

        public CustomMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        // IMyScopedService is injected into Invoke
        public async Task Invoke(HttpContext httpContext)
        {
            await _next(httpContext);
        }
    }
}