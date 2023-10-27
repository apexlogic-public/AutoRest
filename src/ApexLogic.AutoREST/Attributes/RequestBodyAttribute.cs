using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApexLogic.AutoREST
{
    /// <summary>
    /// Indicates that the parameter value will be sent as the response body in <see cref="HttpVerb.POST" />/<see cref="HttpVerb.PUT" />/<see cref="HttpVerb.PATCH" /> etc. connections.
    /// Contents will be serialized into a JSON object.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    public class RequestBodyAttribute : Attribute
    {
    }
}
