using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace MsgNThen.Adapter
{
    public class DefaultSessionFeature : ISessionFeature
    {
        public ISession Session { get; set; }
    }
}