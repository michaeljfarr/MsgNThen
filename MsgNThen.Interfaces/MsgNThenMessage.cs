using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace MsgNThen.Interfaces
{
    public class MsgNThenMessage
    {
        public int StatusCode { get; set; }

        public string ReasonPhrase { get; set; }

        public IHeaderDictionary Headers { get; set; }

        public Stream Body { get; set; }

    }
}
