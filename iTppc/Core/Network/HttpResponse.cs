using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iTppc.Core.Network
{
    public class HttpResponse
    {
        public HttpResponse(string html, string url)
        {
            Html = html;
            Url = url;
        }

        public string Html { get; private set; }

        public string Url { get; private set; }
    }
}
