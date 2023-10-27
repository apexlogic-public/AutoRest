using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ApexLogic.AutoREST;
using ApexLogic.AutoREST.Utils;
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
            return ClientUtils.RestApiEventCreator("http://localhost:8000", method);
        }


        public static object RestApiCallHandler(string method, Type returnType, Dictionary<string, object> parameters)
        {
            return ClientUtils.RestApiCallHandler("http://localhost:8000", client, method, returnType, parameters);
        }
    }
}
