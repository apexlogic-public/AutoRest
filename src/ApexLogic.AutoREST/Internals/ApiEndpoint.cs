using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ApexLogic.AutoREST.Internals
{
    internal class ApiEndpoint
    {
        internal enum EndpointTypes
        {
            Method,
            EventSubscribe,
            EventUnsubscribe
        }

        public EndpointTypes Type { get; set; }
        public string Route { get; set; }
        public HttpVerb Verb { get; set; }
        public object ServiceObject { get; set; }
        public MemberInfo RelfectionInfo { get; set; }
    }
}
