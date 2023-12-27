﻿using ApexLogic.AutoREST;
using Newtonsoft.Json;

using Sample.Client;
using Sample.Server;

using System.Collections.Specialized;
using System.Data.SqlTypes;
using System.Net;
using System.Reflection;
using System.Text;

namespace Sample
{
    class Program
    {
        public static void Main(string[] args)
        {
            //Check ISampleApi.cs for details on the features!

            #region Server creation
            //Create a service - this will be  aset of endpoint on the server. You have to implement it yourself. A backing interface is highly recommended.
            SampleService service = new SampleService();

            //Create a HTTP server and bing it toa host address
            RestApiServer server = new RestApiServer();
            server.Init("http://localhost:8000/");

            //Add our service to the server
            server.Register(service);

            #endregion

            #region Client creation
            //Using an interface and two delegates we can generate a client object that will correspond to our service.
            RestApi.SampleApi = Implement<ISampleApi>.LikeThis(RestApi.RestApiCallHandler, RestApi.RestApiEventCreator);

            //We can even subscribe to simple forms of events - data during an event raise is no transmitted,
            //but you can use a regular call to query data in an asynchronous way.
            RestApi.SampleApi.SimpleEvent.Subscribe(OnEvent);

            #endregion

            //Calling some sample methods using the autogenerated client - add breakpoint in the service class to see the method being called "remotely".
            string upper = RestApi.SampleApi.ToUpper("parameter value");
            var nums = RestApi.SampleApi.Numbers0To10();
            dynamic test = RestApi.SampleApi.DynamicData();
            RestApi.SampleApi.SendLotsOfData("test");

            //Raise some events...
            while (true)
            {
                service.RaiseSimpleEvent();
                Thread.Sleep(3000);
            }
        }

        public static void OnEvent(object sender, EventArgs e)
        {
            //...handling raised events
            Console.WriteLine("Client event sunscription!");
        }
    }
}