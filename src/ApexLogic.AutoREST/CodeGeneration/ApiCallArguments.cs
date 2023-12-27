using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ApexLogic.AutoREST
{
    public class ApiCallArguments
    {
        public string Host { get; set; } 
        public HttpVerb Verb { get; }

        public string Method { get; }
        public Type ReturnType { get; }

        public Dictionary<string, object> Parameters { get; }
        public object RequestBody { get; }

        public ApiCallArguments(string host, HttpVerb verb, string method, Type returnType, Dictionary<string, object> parameters, object body)
        {
            Host = host;
            Verb = verb;
            Method = method;
            Parameters = parameters;
            RequestBody = body;
            ReturnType = returnType;
        }
    }
}
