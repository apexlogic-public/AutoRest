using ApexLogic.AutoREST;
using RestServer;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestServer
{
    [RestApi("api/sample")]
    public interface ISampleApi
    {
        void Test(string param1);

        string ToUpper(string value);

        List<int> Numbers0To10();

        dynamic DynamicData();

        ServerSideEvent SimpleEvent { get; }

        [UseHttpMethod(HttpVerb.POST)]
        void SendLotsOfData([RequestBody] string data);
    }
}
