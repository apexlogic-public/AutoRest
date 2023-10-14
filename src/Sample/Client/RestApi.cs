using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ApexLogic.AutoREST;
using Newtonsoft.Json;


namespace RestServer.Client
{
    public class RestApi
    {
        public static ISampleApi SampleApi { get; set; }
        //public static IMachineApi MachineApi { get; }
        //public static IJobApi JobApi { get; }

        static HttpClient client;

        static RestApi()
        {
            //SampleApi = Implement<ISampleApi>.LikeThis(RestApiCallHandler);
            client = new HttpClient();
        }

        public static ServerSideEvent RestApiEventCreator(string method)
        {
            string s = "http://localhost:8000" + GetRoute().Replace("get_", "");

            return new ServerSideEventClient(s);
        }


        public static object RestApiCallHandler(string method, Type returnType, Dictionary<string, object> parameters)
        {
            string s = "http://localhost:8000" + GetRoute() + GetQuery(parameters);

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
