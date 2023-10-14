using ApexLogic.AutoREST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestServer.Server
{
    internal class SampleService : ISampleApi
    {
        public ServerSideEvent SimpleEvent { get; } = new ServerSideEvent();

        public dynamic DynamicData()
        {
            return new Dictionary<string, int>()
            {
                { "Dzsézönsztetem", 100 }
            };
        }

        public List<int> Numbers0To10()
        {
            Console.WriteLine("Numbers0To10 called on server!");
            List<int> result = new List<int>();
            for(int i = 0; i < 10; i++)
            {
                result.Add(i);
            }
            return result;
        }

        public void Test(string param1)
        {
            Console.WriteLine("Call to server successful!");
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
    }
}
