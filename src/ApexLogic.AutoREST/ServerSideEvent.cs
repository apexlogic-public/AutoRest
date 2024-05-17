using ApexLogic.AutoREST.Internals.TransportLayer;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
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
	public abstract class ServerSideEventBase
	{
		public const string NAME_FIELD = nameof(PropertyName);
		public const string SERVICE_FIELD = nameof(Service);
		protected string PropertyName { get; set; }
		protected object Service { get; set; }

		internal abstract Type EventHandlerType { get; }
		internal abstract void SubscribeGeneric(EventHandler<object> handler);
		internal abstract void UnsubscribeGeneric(EventHandler<object> handler);
	}

	public abstract class ServerSideEventBase<TEvent, TArgs> : ServerSideEventBase
	{
		public class EventInvocationData
		{
			public TEvent Invokable { get; }
			public EventHandler<object> SystemSubscriber { get; }

			public EventInvocationData(TEvent subscriber)
			{
				Invokable = subscriber;
				SystemSubscriber = null;
			}

			public EventInvocationData(TEvent subscriber, EventHandler<object> original)
			{
				Invokable = subscriber;
				SystemSubscriber = original;
			}
		}
		protected List<EventInvocationData> _invocationList = new List<EventInvocationData>();

		internal override Type EventHandlerType => typeof(TEvent);

		public static ServerSideEventBase<TEvent, TArgs> operator +(ServerSideEventBase<TEvent, TArgs> evnt, TEvent subscriber)
		{
			evnt.Subscribe(subscriber);
			return evnt;
		}

		public static ServerSideEventBase<TEvent, TArgs> operator -(ServerSideEventBase<TEvent, TArgs> evnt, TEvent subscriber)
		{
			evnt.Unsubscribe(subscriber);
			return evnt;
		}

		public virtual void Subscribe(TEvent subscriber)
		{
			_invocationList.Add(new EventInvocationData(subscriber));
		}

		public virtual void Unsubscribe(TEvent subscriber)
		{
			EventInvocationData data = _invocationList.FirstOrDefault(e => e.Invokable.Equals(subscriber));
			if(data != null)
			{
				_invocationList.Remove(data);
			}	
		}

        public abstract void Invoke(object sender, TArgs e);
	}

	public class ServerSideEvent : ServerSideEventBase<EventHandler, EventArgs>
	{
		public override void Invoke(object sender, EventArgs e)
		{
			foreach (EventInvocationData handler in _invocationList)
			{
				if (handler.SystemSubscriber != null)
				{
					handler.Invokable(new Tuple<object, string>(Service, PropertyName), e);
				}
				else
				{
					handler.Invokable(sender, e);
				}
			}
		}

		internal override void SubscribeGeneric(EventHandler<object> handler)
		{
			_invocationList.Add(new EventInvocationData(new EventHandler(handler), handler));
		}

		internal override void UnsubscribeGeneric(EventHandler<object> handler)
		{
			EventInvocationData data = _invocationList.FirstOrDefault(e => e.SystemSubscriber.Equals(handler));
			if (data != null)
			{
				_invocationList.Remove(data);
			}
		}
	}

	public class ServerSideEvent<T> : ServerSideEventBase<EventHandler<T>, T>
    {
		public override void Invoke(object sender, T e)
		{
			foreach (EventInvocationData handler in _invocationList)
			{
				if (handler.SystemSubscriber != null)
				{
					handler.Invokable(new Tuple<object, string>(Service, PropertyName), e);
				}
				else
				{
					handler.Invokable(sender, e);
				}
			}
		}

		internal override void SubscribeGeneric(EventHandler<object> handler)
		{
			_invocationList.Add(new EventInvocationData(new EventHandler<T>((sender, e) => handler(sender, e)), handler));
		}

		internal override void UnsubscribeGeneric(EventHandler<object> handler)
		{
			EventInvocationData data = _invocationList.FirstOrDefault(e => e.SystemSubscriber.Equals(handler));
			if (data != null)
			{
				_invocationList.Remove(data);
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
            _invocationList.Add(new EventInvocationData(subscriber));

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
			EventInvocationData data = _invocationList.FirstOrDefault(e => e.Invokable.Equals(subscriber));
			if (data != null)
			{
				_invocationList.Remove(data);
			}
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
                                Invoke(this, EventArgs.Empty);
                            }
                        }
                    }
                }
            }
        }
    }

	public class ServerSideEventClient<T> : ServerSideEvent<T>
	{
		private string _host;
		private bool _listenerStarted;
		private Task _readerTask;

		public ServerSideEventClient(string host)
		{
			_host = host;
		}

		public override void Subscribe(EventHandler<T> subscriber)
		{
			_invocationList.Add(new EventInvocationData(subscriber));

			if (!_listenerStarted)
			{
				//start connection and runner
				_listenerStarted = true;
				_readerTask = new Task(ReadRunner, TaskCreationOptions.LongRunning);
				_readerTask.Start();
			}
		}

		public override void Unsubscribe(EventHandler<T> subscriber)
		{
			EventInvocationData data = _invocationList.FirstOrDefault(e => e.Invokable.Equals(subscriber));
			if (data != null)
			{
				_invocationList.Remove(data);
			}
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
						if (s != "{ }")
						{
							if (s.Contains("\"ServerDateTime\"") && s.Contains("\"EventName\""))
							{
								EventInvoke eventData = JsonConvert.DeserializeObject<EventInvoke>(s);
								T data = JsonConvert.DeserializeObject<T>(eventData.Data);
								Invoke(this, data);
							}
						}
					}
				}
			}
		}

	}
}
