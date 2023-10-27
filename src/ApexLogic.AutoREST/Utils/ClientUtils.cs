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
    public class ClientUtils
    {
        public static ServerSideEvent RestApiEventCreator(string host, string method)
        {
            string s = host + GetRoute().Replace("get_", "");

            return new ServerSideEventClient(s);
        }


        public static object RestApiCallHandler(string host, HttpClient client, string method, Type returnType, Dictionary<string, object> parameters)
        {
            string s = host + GetRoute() + GetQuery(parameters);

            Task<Stream> task = client.GetStreamAsync(s);
            task.Wait();

            string json = "";
            using (StreamReader sr = new StreamReader(task.Result))
            {
                json = sr.ReadToEnd();
            }

            object o = JsonConvert.DeserializeObject(json, returnType);

            return o;
        }

        private static string GetRoute()
        {
            MethodBase method = new StackTrace().GetFrame(2).GetMethod();
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
