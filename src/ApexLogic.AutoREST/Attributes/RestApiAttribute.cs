using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApexLogic.AutoREST
{
    /// <summary>
    /// Indicates that an object marked with this attribute's public, non-ignored methods will be available as REST-api endpoint on the given route.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public class RestApiAttribute : Attribute
    {
        /// <summary>
        /// Route of this service/endpoint collection.
        /// </summary>
        public string EndPoint { get; }

        /// <summary>
        /// Creates a new <see cref="RestApiAttribute"/> with the given route.
        /// </summary>
        /// <param name="path">The route this class's methods will be available as REST endpoints.</param>
        public RestApiAttribute(string path)
        {
            if (!path.StartsWith("/"))
            {
                path = "/" + path;
            }
            EndPoint = path;
        }
    }
}
