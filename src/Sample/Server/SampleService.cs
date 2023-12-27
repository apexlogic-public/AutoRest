using ApexLogic.AutoREST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Server
{
    internal class SampleService : ISampleApi
    {
        public ServerSideEvent SimpleEvent { get; } = new ServerSideEvent();

        public dynamic DynamicData()
        {
            return new Dictionary<string, int>()
            {
                { "Generic Data", 100 }
            };
        }

        public List<int> Numbers0To10()
        {
            List<int> result = new List<int>();
            for(int i = 0; i < 10; i++)
            {
                result.Add(i);
            }
            return result;
        }

        public void Test(string param1)
        {
        }

        public string ToUpper(string value)
        {
            
            return value.ToUpper();
        }

        [RestIgnore]
        public void RaiseSimpleEvent()
        {
            SimpleEvent.Invoke(this, EventArgs.Empty);
        }

        [UseHttpMethod(HttpVerb.POST)]
        public void SendLotsOfData([RequestBody] string data)
        {
            Console.WriteLine("[SERVER] Processing LotsOfData...");
            Task.Delay(500).Wait();
        }
    }
}
