﻿using ApexLogic.AutoREST.Internals;
using ApexLogic.AutoREST.Internals.TransportLayer;
using Newtonsoft.Json;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ApexLogic.AutoREST
{
    /// <summary>
    /// Handles service objects marked with the <see cref="RestApiAttribute"/> attribute for routes and handles inocming HTTP connections, serving them if possible.
    /// </summary>
    public class RestApiServer
    {
        private static readonly string[] _methodNameExclusions = new string[] 
        { 
            nameof(ToString), 
            nameof(GetHashCode), 
            nameof(Equals), 
            nameof(GetType) 
        };

        private static ConcurrentDictionary<int, HttpListenerRequest> _servercallMetadata = new ConcurrentDictionary<int, HttpListenerRequest>();

        private List<ApiEventSubscription> _eventSupscriptions = new List<ApiEventSubscription>();

        private bool _isRunning;
        private HttpListener _listener;
        private string _url = "http://localhost:8000/";

        private Thread _requestThread;
        private Thread _eventThread;

        private List<ApiEndpoint> endpoints { get; } = new List<ApiEndpoint>();

        /// <summary>
        /// Creates a new <see cref="RestApiServer"/>.
        /// </summary>
        public RestApiServer()
        {

        }

        /// <summary>
        /// Initializes the server and starts listening on the supplied host address.
        /// </summary>
        /// <param name="host">The working host address. Use http:// or https:// with port inidciation (e.g. :8080)</param>
        public void Init(string host)
        {
            _url = host;

            _isRunning = true;

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

        /// <summary>
        /// Stops listening in the supplied host address and stops related threads.
        /// </summary>
        public void Close()
        {
            _isRunning = false;
            _listener.Stop();
        }

        /// <summary>
        /// Registers a service object  marked with the <see cref="RestApiAttribute"/> attribute adding its public non-ignored methods to the available endpoints.
        /// </summary>
        /// <param name="api">The service object to be registered.</param>
        public void Register(object api)
        {
            Type type = api.GetType();
            RestApiAttribute[] apiAttr = type.GetCustomAttributes<RestApiAttribute>(true);
            ResponseHeaderAttribute[] globalHeadersAttr = type.GetCustomAttributes<ResponseHeaderAttribute>(true);

            if (apiAttr.Length > 0)
            {
                string baseAddress = apiAttr[0].EndPoint;

                foreach (MethodInfo method in type.GetMethods())
                {
                    RestIgnoreAttribute ingoreAttr = method.GetCustomAttribute<RestIgnoreAttribute>();
                    UseHttpMethodAttribute methodAttr = method.GetCustomAttribute<UseHttpMethodAttribute>();
					ResponseHeaderAttribute[] localHeadersAttr = method.GetCustomAttributes<ResponseHeaderAttribute>(true).ToArray();

                    Dictionary<string, string> headers = new Dictionary<string, string>();
                    foreach(ResponseHeaderAttribute attr in globalHeadersAttr)
                    {
                        headers.Add(attr.Header, attr.Value);
					}
                    foreach(ResponseHeaderAttribute attr in localHeadersAttr)
                    {
                        headers.Add(attr.Header, attr.Value);
					}

					if (ingoreAttr == null && method.IsPublic && !_methodNameExclusions.Contains(method.Name))
                    {
                        HttpVerb verb = methodAttr != null ? methodAttr.Method : HttpVerb.GET;
                        endpoints.Add(new ApiEndpoint()
                        {
                            Type = ApiEndpoint.EndpointTypes.Method,
                            Verb = verb,
                            Route = $"{baseAddress}/{method.Name.ToLower()}",
                            ServiceObject = api,
                            RelfectionInfo = method,
                            Headers = headers
						});
                    }
                }

                foreach (PropertyInfo property in type.GetProperties().Where(prop => typeof(ServerSideEventBase).IsAssignableFrom(prop.PropertyType)))
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

                        object evtRaw = property.GetValue(api);
						ServerSideEventBase evt = evtRaw as ServerSideEventBase;
                        evt.SubscribeGeneric(OnEvent);
						typeof(ServerSideEventBase).GetProperty(ServerSideEventBase.NAME_FIELD, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(evt, property.Name);
						typeof(ServerSideEventBase).GetProperty(ServerSideEventBase.SERVICE_FIELD, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(evt, api);
                    }
                }
            }
        }

        /// <summary>
        /// Un-registers a service object  marked with the <see cref="RestApiAttribute"/> attribute removing its public non-ignored methods from the available endpoints.
        /// </summary>
        /// <param name="api">The service object to be un-registered.</param>
        public void Unregister(object api)
        {
            foreach(ApiEndpoint endpoint in endpoints.Where(ep => ep.ServiceObject == api))
            {
                endpoints.Remove(endpoint);
            }       
        }

        /// <summary>
        /// Allows access to a <see cref="HttpListenerRequest"/> instance in a transient service method.
        /// </summary>
        /// <returns>The request data for the incoming request.</returns>
        public static HttpListenerRequest GetHttpRequestData()
        {
            if(_servercallMetadata.ContainsKey((int)Task.CurrentId))
            {
                return _servercallMetadata[(int)Task.CurrentId];
            }
            return null;
        }

        private void RequestRunner()
        {
            while (_isRunning)
            {
                HttpListenerContext ctx = _listener.GetContext();

                Task run = null;
                run = Task.Run(() =>
                {
                    HttpListenerRequest req = ctx.Request;
                    HttpListenerResponse resp = ctx.Response;

                    while (!_servercallMetadata.TryAdd((int)Task.CurrentId, req)) ;

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

                    while (run == null && !_servercallMetadata.TryRemove(run.Id, out _)) ;
                });
            }

        }

        private void EventKeepaliveRunner()
        {
            while (_isRunning)
            {
                //List<ApiEventSubscription> killedSubscriptions = new List<ApiEventSubscription>();

                foreach(ApiEventSubscription subscription in _eventSupscriptions)
                {
                    lock (subscription)
                    {
                        string result = JsonConvert.SerializeObject(new KeepAlive());

                        byte[] data = Encoding.UTF8.GetBytes(result + Environment.NewLine);

                        try
                        {
							subscription.Client.Response.OutputStream.Write(data, 0, data.Length);
							subscription.Client.Response.OutputStream.Flush();
						}
						catch (HttpListenerException)
                        {
                            //killedSubscriptions.Add(subscription);
						}
                    }
                }

                Thread.Sleep(5000);
            }
        }

        private void HandleMethodCall(ApiEndpoint endpoint, HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                string body = string.Empty;
                if(request.HasEntityBody)
                {
                    using(StreamReader sr = new StreamReader(request.InputStream))
                    {
                        body = sr.ReadToEnd();
                    }
                }
                object[] param = FillParameters(endpoint.RelfectionInfo as MethodInfo, request.QueryString, body);
                object returnData = (endpoint.RelfectionInfo as MethodInfo).Invoke(endpoint.ServiceObject, param);
                string result = JsonConvert.SerializeObject(returnData);

                foreach(KeyValuePair<string, string> header in endpoint.Headers)
                {
                    response.Headers.Add(header.Key, header.Value);
                }

                response.StatusCode = 200;
                byte[] data = Encoding.UTF8.GetBytes(result);
                response.ContentLength64 = data.LongLength;
                response.OutputStream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;

                string result = JsonConvert.SerializeObject(ex.InnerException);
                byte[] data = Encoding.UTF8.GetBytes(result);
                response.ContentLength64 = data.LongLength;
                response.OutputStream.Write(data, 0, data.Length);
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

            ctx.Response.StatusCode = 200;
            ctx.Response.Headers.Add("Content-Type", "text/event-stream");
            ctx.Response.Headers.Add("Cache-Control", "no-cache");
            ctx.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            ctx.Response.Headers.Add("Content-Encoding", "none");

            ctx.Response.KeepAlive = true;

            string rawData = "event: Event\n\n";
            byte[] data = Encoding.UTF8.GetBytes(rawData);
            ctx.Response.OutputStream.Write(data, 0, data.Length);

        }

        private void OnEvent(object sender, object e)
        {
            List<ApiEventSubscription> deadSubs = new List<ApiEventSubscription>();

            Tuple<object, string> evt = sender as Tuple<object, string>;
            foreach(ApiEventSubscription subscription in GetEventSubscriptions(evt))
            {
                lock(subscription)
                {
                    string result = JsonConvert.SerializeObject(new EventInvoke()
                    {
                        ServerDateTime = DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
                        EventName = evt.Item2,
                        Data = JsonConvert.SerializeObject(e)
                    });

                    subscription.Client.Response.Headers.Add("Content-Type", "text/event-stream");


                    string rawData = "data: " + result + "\n\n";
                    byte[] data = Encoding.UTF8.GetBytes(rawData);

                    try
                    {
                        subscription.Client.Response.OutputStream.Write(data, 0, data.Length);
						subscription.Client.Response.OutputStream.Flush();
					}
                    catch(HttpListenerException ex)
                    {
						deadSubs.Add(subscription);
					}               
                }
            }

            _eventSupscriptions = _eventSupscriptions.Except(deadSubs).ToList();
        }

        private IEnumerable<ApiEventSubscription> GetEventSubscriptions(Tuple<object, string> evt)
        {
            return _eventSupscriptions.Where(sub => sub.Endpoint.ServiceObject == evt.Item1 && sub.Endpoint.Route.EndsWith($"{evt.Item2.ToLower()}/subscribe"));
        }

        private object[] FillParameters(MethodInfo method, NameValueCollection query, string requestBody)
        {
            List<object> result = new List<object>();
            foreach (ParameterInfo parameter in method.GetParameters())
            {
                RequestBodyAttribute reqBody = parameter.GetCustomAttributes<RequestBodyAttribute>(true).FirstOrDefault();

                if (reqBody != null)
                {
                    result.Add(JsonConvert.DeserializeObject(requestBody, parameter.ParameterType));
                }
                else
                {
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
                        throw new ArgumentException($"Cannot fill required parameter: {parameter.Name}!");
                    }
                }
            }

            return result.ToArray();
        }
    }
}
