using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ApexLogic.AutoREST;
using ApexLogic.AutoREST.CodeGeneration;
using ApexLogic.AutoREST.Utils;
using Newtonsoft.Json;


namespace Sample.Client
{
    public class RestApi
    {
        //You can also add other APIs if needed
        public static ISampleApi SampleApi { get; set; }

        static HttpClient client;

        static RestApi()
        {
            client = new HttpClient();
        }

        public static ServerSideEvent RestApiEventCreator(string method)
        {
            return ClientUtils.RestApiEventCreator("http://localhost:8000", method);
        }

        public static object RestApiCallHandler(ApiCallArguments args)
        {
            args.Host = "http://localhost:8000";
            return ClientUtils.RestApiCallHandler(args, client);
        }
    }
}
