using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ApexLogic.AutoREST
{
    /// <summary>
    /// Indicates what HTTP method the call with this method/endpoint will use.
    /// </summary>
    public class UseHttpMethodAttribute : Attribute
    {
        /// <summary>
        /// The used <see cref="HttpVerb"/>.
        /// </summary>
        public HttpVerb Method { get; }

        /// <summary>
        /// Creates a new <see cref="UseHttpMethodAttribute"/> with the given HTTP method.
        /// </summary>
        /// <param name="method">The used <see cref="HttpVerb"/>.</param>
        public UseHttpMethodAttribute(HttpVerb method)
        {
            Method = method;
        }
    }
}
