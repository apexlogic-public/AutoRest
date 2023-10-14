using ApexLogic.AutoREST;
using Newtonsoft.Json;

using RestServer.Client;
using RestServer.Server;

using System.Collections.Specialized;
using System.Data.SqlTypes;
using System.Net;
using System.Reflection;
using System.Text;

namespace RestServer
{
    class RestServer
    {
        public static void Main(string[] args)
        {
            #region Server creation

            SampleService service = new SampleService();
            RestApiServer server = new RestApiServer();
            server.Init("http://localhost:8000/");
            server.Register(service);

            #endregion

            service.ToUpper("test");


            #region Client creation

            RestApi.SampleApi = Implement<ISampleApi>.LikeThis(RestApi.RestApiCallHandler, RestApi.RestApiEventCreator);
            RestApi.SampleApi.SimpleEvent.Subscribe(OnEvent);

            #endregion

            //string upper = RestApi.SampleApi.ToUpper("parameter value");
            //var nums = RestApi.SampleApi.Numbers0To10();
            //dynamic test = RestApi.SampleApi.DynamicData();

            while (true)
            {
                service.RaiseSimpleEvent();
                Thread.Sleep(3000);
            }
        }

        public static void OnEvent(object sender, EventArgs e)
        {
            Console.WriteLine("Client event sunscription!");
        }
    }
}