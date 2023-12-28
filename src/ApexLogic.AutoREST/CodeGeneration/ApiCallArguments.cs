using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ApexLogic.AutoREST
{
    /// <summary>
    /// Hold arguments on how to call REST API endpoints and what response to expect.
    /// </summary>
    public class ApiCallArguments
    {
        /// <summary>
        /// The address of the host.
        /// </summary>
        public string Host { get; set; } 

        /// <summary>
        /// The HTTP method/verb to use.
        /// </summary>
        public HttpVerb Verb { get; }

        /// <summary>
        /// Name of the REST API method (correlated to the service method name).
        /// </summary>
        public string Method { get; }

        /// <summary>
        /// Expected response type. Can be any JSON-serializable C# type, including the special type <see cref="System.Void"/>.
        /// </summary>
        public Type ReturnType { get; }

        /// <summary>
        /// The used query/URL parameters.
        /// </summary>
        public Dictionary<string, object> Parameters { get; }

        /// <summary>
        /// The request body. Used with verbs <see cref="HttpVerb.POST"/> and <see cref="HttpVerb.DELETE"/>. Will be serialized and forwarded into the method parameter
        /// designated with the <see cref="RequestBodyAttribute"/> attribute.
        /// </summary>
        public object RequestBody { get; }

        /// <summary>
        /// Creates a new <see cref="ApiCallArguments"/>.
        /// </summary>
        /// <param name="host"><inheritdoc cref="Host" path="/summary"/></param>
        /// <param name="verb"><inheritdoc cref="Verb" path="/summary"/></param>
        /// <param name="method"><inheritdoc cref="Method" path="/summary"/></param>
        /// <param name="returnType"><inheritdoc cref="ReturnType" path="/summary"/></param>
        /// <param name="parameters"><inheritdoc cref="Parameters" path="/summary"/></param>
        /// <param name="body"><inheritdoc cref="RequestBody" path="/summary"/></param>
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
