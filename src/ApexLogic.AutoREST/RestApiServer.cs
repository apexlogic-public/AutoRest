﻿using ApexLogic.AutoREST.Internals;
using ApexLogic.AutoREST.Internals.TransportLayer;
using Newtonsoft.Json;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ApexLogic.AutoREST
{
    public class RestApiServer
    {
        private readonly List<string> MethodNameExclusions = new List<string>() { "ToString", "GetHashCode", "Equals", "GetType" };

        private ConcurrentBag<ApiEventSubscription> _eventSupscriptions = new ConcurrentBag<ApiEventSubscription>();

        private HttpListener _listener;
        private string _url = "http://localhost:8000/";

        private Thread _requestThread;
        private Thread _eventThread;

        static List<ApiEndpoint> endpoints { get; } = new List<ApiEndpoint>();

        public void Init(string host)
        {
            _url = host;

            _listener = new HttpListener();
            _listener.Prefixes.Add(_url);
            _listener.Start();

            _requestThread = new Thread(new ThreadStart(RequestRunner))
            {
                IsBackground = true,
                Name = "Rest Server thread"
            };
            _requestThread.Start();

            _eventThread = new Thread(new ThreadStart(EventKeepaliveRunner))
            {
                IsBackground = true,
                Name = "Rest Server event keepalive thread"
            };
            _eventThread.Start();
        }

        public void Register(object api)
        {
            Type type = api.GetType();
            RestApiAttribute[] apiAttr = type.GetCustomAttributes<RestApiAttribute>(true);

            if (apiAttr.Length > 0)
            {
                string baseAddress = apiAttr[0].EndPoint;

                foreach (MethodInfo method in type.GetMethods())
                {
                    RestIgnoreAttribute attr = method.GetCustomAttribute<RestIgnoreAttribute>();
                    if (attr == null && method.IsPublic && !MethodNameExclusions.Contains(method.Name))
                    {
                        endpoints.Add(new ApiEndpoint()
                        {
                            Type = ApiEndpoint.EndpointTypes.Method,
                            Route = $"{baseAddress}/{method.Name.ToLower()}",
                            ServiceObject = api,
                            RelfectionInfo = method
                        });
                    }
                }

                foreach (PropertyInfo property in type.GetProperties().Where(prop => prop.PropertyType == typeof(ServerSideEvent)))
                {
                    RestIgnoreAttribute attr = property.GetCustomAttribute<RestIgnoreAttribute>();
                    if (attr == null)
                    {
                        endpoints.Add(new ApiEndpoint()
                        {
                            Type = ApiEndpoint.EndpointTypes.EventSubscribe,
                            Route = $"{baseAddress}/{property.Name.ToLower()}/subscribe",
                            ServiceObject = api,
                            RelfectionInfo = property
                        });
                        endpoints.Add(new ApiEndpoint()
                        {
                            Type = ApiEndpoint.EndpointTypes.EventUnsubscribe,
                            Route = $"{baseAddress}/{property.Name.ToLower()}/unsubscribe",
                            ServiceObject = api,
                            RelfectionInfo = property
                        });

                        ServerSideEvent evt = (property.GetValue(api) as ServerSideEvent);
                        evt.Subscribe(OnEvent);
                        typeof(ServerSideEvent).GetProperty(ServerSideEvent.NAME_FIELD, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(evt, property.Name);
                        typeof(ServerSideEvent).GetProperty(ServerSideEvent.SERVICE_FIELD, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(evt, api);
                    }
                }
            }
        }

        private async void RequestRunner()
        {
            while (true)
            {
                HttpListenerContext ctx = await _listener.GetContextAsync();

                _ = Task.Run(async () =>
                {
                    HttpListenerRequest req = ctx.Request;
                    HttpListenerResponse resp = ctx.Response;

                    resp.ContentType = "text/json";
                    resp.ContentEncoding = Encoding.UTF8;

                    string path = req.Url.AbsolutePath;
                    ApiEndpoint endpoint = endpoints.FirstOrDefault(ep => ep.Route == path);
                    if (endpoint != null)
                    {
                        switch(endpoint.Type)
                        {
                            case ApiEndpoint.EndpointTypes.Method: HandleMethodCall(endpoint, req, resp); break;
                            case ApiEndpoint.EndpointTypes.EventSubscribe: SubscribeToEvent(endpoint, ctx); break;
                        }
                    }
                    else
                    {
                        resp.StatusCode = 404;
                        resp.Close();
                    }
                });
            }

        }

        private void EventKeepaliveRunner()
        {
            while (true)
            {
                foreach(ApiEventSubscription subscription in _eventSupscriptions)
                {
                    lock (subscription)
                    {
                        string result = JsonConvert.SerializeObject(new KeepAlive());

                        byte[] data = Encoding.UTF8.GetBytes(result + Environment.NewLine);
                        subscription.Client.Response.OutputStream.Write(data, 0, data.Length);

                        subscription.Client.Response.OutputStream.Flush();
                    }
                }
                Thread.Sleep(5000);
            }
        }

        private void HandleMethodCall(ApiEndpoint endpoint, HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                object[] param = FillParameters(endpoint.RelfectionInfo as MethodInfo, request.QueryString);
                object returnData = (endpoint.RelfectionInfo as MethodInfo).Invoke(endpoint.ServiceObject, param);
                string result = JsonConvert.SerializeObject(returnData);

                response.StatusCode = 200;
                byte[] data = Encoding.UTF8.GetBytes(result);
                response.ContentLength64 = data.LongLength;
                response.OutputStream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
            }

            response.Close();
        }

        private void SubscribeToEvent(ApiEndpoint endpoint, HttpListenerContext ctx)
        {
            _eventSupscriptions.Add(new ApiEventSubscription()
            {
                Endpoint = endpoint,
                Client = ctx
            });

            ctx.Response.ContentType = "text/event";
            ctx.Response.ContentEncoding = Encoding.UTF8;

            ctx.Response.KeepAlive = true;
        }

        private void OnEvent(object sender, EventArgs e)
        {
            Tuple<object, string> evt = sender as Tuple<object, string>;
            foreach(ApiEventSubscription subscription in GetEventSubscriptions(evt))
            {
                lock(subscription)
                {
                    string result = JsonConvert.SerializeObject(new EventInvoke()
                    {
                        ServerDateTime = DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
                        EventName = evt.Item2
                    });

                    byte[] data = Encoding.UTF8.GetBytes(result + Environment.NewLine);
                    subscription.Client.Response.OutputStream.Write(data);

                    subscription.Client.Response.OutputStream.Flush();
                }
            }
            Console.WriteLine("[Server] Event invoked");
        }

        private IEnumerable<ApiEventSubscription> GetEventSubscriptions(Tuple<object, string> evt)
        {
            return _eventSupscriptions.Where(sub => sub.Endpoint.ServiceObject == evt.Item1 && sub.Endpoint.Route.EndsWith($"{evt.Item2.ToLower()}/subscribe"));
        }

        private object[] FillParameters(MethodInfo method, NameValueCollection query)
        {
            List<object> result = new List<object>();
            foreach (ParameterInfo parameter in method.GetParameters())
            {
                object value = null;
                string queryValue = query[parameter.Name];
                if (queryValue != null)
                {
                    result.Add(Convert.ChangeType(queryValue, parameter.ParameterType));
                }
                else if (parameter.HasDefaultValue)
                {
                    result.Add(parameter.DefaultValue);
                }
                else
                {
                    throw new ArgumentException("Cannot fill required parameter!");
                }
            }

            return result.ToArray();
        }
    }
}
