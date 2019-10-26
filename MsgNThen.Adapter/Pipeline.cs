using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using MsgNThen.Interfaces;

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
    public class Pipeline : IMessageHandler
    {
        public void Foo()
        {
            //andThenPublisher.BindDirectQueue("a_ex", "a");
            //andThenPublisher.BindDirectQueue("b_ex", "b");
            //andThenPublisher.BindDirectQueue("c_ex", "c");
            //andThenPublisher.BindDirectQueue("d_ex", "d");
            //listener.Listen("a", 2);
            //listener.Listen("b", 2);
            //listener.Listen("c", 2);
            //listener.Listen("d", 2);

        }

        //The goal is to create a generic pipeline for queues that can implement tasks before, after and around queue based message 
        //handlers
        public Task<AckResult> HandleMessageTask(IHandledMessage message)
        {
            throw new NotImplementedException();
        }
    }
}
