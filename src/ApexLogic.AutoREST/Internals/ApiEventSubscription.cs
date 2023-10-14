using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ApexLogic.AutoREST.Internals
{
    internal class ApiEventSubscription
    {
        public ApiEndpoint Endpoint { get; set; }
        public HttpListenerContext Client { get; set; }
    }
}
