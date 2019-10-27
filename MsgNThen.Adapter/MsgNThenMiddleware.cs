using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MsgNThen.Adapter
{
    public class MsgNThenMiddleware
    {
        private readonly RequestDelegate _next;

        public MsgNThenMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            //send I started it
            await _next(context);
            //send I handled it
        }
    }
}