using ApexLogic.AutoREST.CodeGeneration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ApexLogic.AutoREST.Utils
{
    /// <summary>
    /// The <see cref="ClientUtils"/> class contains sample implementations for the 
    /// <see cref="Implement{T}.LikeThis(Func{ApiCallArguments, object}, Func{string, ServerSideEvent})"/> constructor delegates.
    /// For specialized needs you can implement these in any way you like, but for common use-cases it is recommended to you these
    /// implementations.
    /// </summary>
    public class ClientUtils
    {
        /// <summary>
        /// Sample implementation for the "event creator" delegate used in the <see cref="Implement{T}"/> class' constructor.
        /// </summary>
        /// <param name="host">The address of the remote server.</param>
        /// <param name="method">The event's name.</param>
        /// <returns>A client-use instance of a <see cref="ServerSideEvent"/>.</returns>
        public static ServerSideEvent RestApiEventCreator(string host, string method)
        {
            string s = host + GetRoute().Replace("get_", "");

            return new ServerSideEventClient(s);
        }

        /// <summary>
        /// Sample implementation for the "method handler" delegate used in the <see cref="Implement{T}"/> class' constructor.
        /// </summary>
        /// <param name="args">Arguments of the remote call (like host, query parameters, request body, HTTP verb etc.).</param>
        /// <param name="client">The <see cref="HttpClient"/> used for the request.</param>
        /// <returns>A JSON-deserialized object based on the server response (the type is designated in <see cref="ApiCallArguments.ReturnType"/>.</returns>
        /// <exception cref="NotSupportedException"></exception>
        public static object RestApiCallHandler(ApiCallArguments args, HttpClient client)
        {
            string s = args.Host + GetRoute() + GetQuery(args.Parameters);

            Task<HttpResponseMessage> task = null;
            switch (args.Verb)
            {
                case HttpVerb.GET:
                    task = client.GetAsync(s);
                    break;
                case HttpVerb.POST:
                    task = client.PostAsync(s, new StringContent(JsonConvert.SerializeObject(args.RequestBody)));
                    break;
                case HttpVerb.PUT:
                    task = client.PutAsync(s, new StringContent(JsonConvert.SerializeObject(args.RequestBody)));
                    break;
                case HttpVerb.DELETE:
                    task = client.DeleteAsync(s);
                    break;
                case HttpVerb.PATCH:
                case HttpVerb.CONNECT:
                case HttpVerb.TRACE:
                case HttpVerb.HEAD:
                case HttpVerb.OPTIONS:
                    throw new NotSupportedException($"HTTP Verb {args.Verb} is not yet supported!");
            }

            task.Wait();

            Task<string> response = task.Result.Content.ReadAsStringAsync();
            response.Wait();

            object result = null;

            if (!args.ReturnType.Equals(typeof(void)))
            {
                result = JsonConvert.DeserializeObject(response.Result, args.ReturnType);
            }

            return result;
        }

        private static string GetRoute(int frame = 3)
        {
            MethodBase method = new StackTrace().GetFrame(frame).GetMethod();
            Type implementedType = method.DeclaringType;
            string result = implementedType.GetCustomAttributes<RestApiAttribute>(true)[0].EndPoint + "/" + method.Name;
            if (!result.StartsWith("/"))
            {
                result = "/" + result;
            }
            return result.ToLower();
        }

        private static string GetQuery(Dictionary<string, object> parameters)
        {
            string result = string.Empty;
            if (parameters.Count > 0)
            {
                result += "?";
                result += string.Join("&", parameters.Select(kvp => $"{kvp.Key}={kvp.Value.ToString()}"));
            }
            return result;
        }

    }
}
