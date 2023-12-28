using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ApexLogic.AutoREST
{
    public class ServerSideEvent
    {
        public const string NAME_FIELD = nameof(PropertyName);
        public const string SERVICE_FIELD = nameof(Service);
        private string PropertyName { get; set; }
        private object Service { get; set; }

        protected List<EventHandler> _invocationList = new List<EventHandler>();

        public static ServerSideEvent operator +(ServerSideEvent evnt, EventHandler subscriber)
        {
            evnt.Subscribe(subscriber);
            return evnt;
        }

        public static ServerSideEvent operator -(ServerSideEvent evnt, EventHandler subscriber)
        {
            evnt.Unsubscribe(subscriber);
            return evnt;
        }

        public virtual void Subscribe(EventHandler subscriber)
        {
            _invocationList.Add(subscriber);
        }

        public virtual void Unsubscribe(EventHandler subscriber)
        {
            _invocationList.Remove(subscriber);
        }

        public void Invoke(object sender, EventArgs e)
        {
            foreach (EventHandler handler in _invocationList)
            {
                if(handler.Target is RestApiServer)
                {
                    handler(new Tuple<object, string>(Service, PropertyName), e);
                }
                else
                {
                    handler(sender, e);
                }
            }
        }

    }

    public class ServerSideEventClient : ServerSideEvent
    {
        private string _host;
        private bool _listenerStarted;
        private Task _readerTask;

        public ServerSideEventClient(string host)
        {
            _host = host;
        }

        public override void Subscribe(EventHandler subscriber)
        {
            _invocationList.Add(subscriber);

            if(!_listenerStarted)
            {
                //start connection and runner
                _listenerStarted = true;
                _readerTask = new Task(ReadRunner, TaskCreationOptions.LongRunning);
                _readerTask.Start();
            }
        }

        public override void Unsubscribe(EventHandler subscriber)
        {
            _invocationList.Remove(subscriber);
        }

        private void ReadRunner()
        {
            using (var client = new HttpClient())
            {
                Task<Stream> task = client.GetStreamAsync($"{_host}/subscribe");
                task.Wait();

                using (var reader = new StreamReader(task.Result))
                {
                    while (true)
                    {
                        string s = reader.ReadLine();
                        if(s != "{ }")
                        {
                            if(s.Contains("\"ServerDateTime\"") && s.Contains("\"EventName\""))
                            {
                                Console.WriteLine("[CLIENT EVENT NOTIFICAITON]" + s);
                                Invoke(this, EventArgs.Empty);
                            }
                        }
                    }
                }
            }
        }
    }
}
