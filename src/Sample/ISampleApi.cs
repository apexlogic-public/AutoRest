using ApexLogic.AutoREST;
using Sample;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample
{
    [RestApi("api/sample")]
    public interface ISampleApi
    {
        //Events are not supported in the classical sense, but you can get similar funcionality using a ServerSideEvent.
        ServerSideEvent SimpleEvent { get; }

        //You can have parameterless, parameterized methods, method with or without return values, dynamic values and use even simple generics Like List or Dictionary.
        void Test(string param1);
        string ToUpper(string value);
        List<int> Numbers0To10();
        dynamic DynamicData();

        //Using different HttpVerbs you can also send large amounts of data with any datatype that is JSON-serializable.
        [UseHttpMethod(HttpVerb.POST)]
        void SendLotsOfData([RequestBody] string data);

        //Using the RestIgnoreAttribute you can have public methods on the interface that are not part of the REST API.
        [RestIgnore]
        void RaiseSimpleEvent();
    }
}
