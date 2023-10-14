using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApexLogic.AutoREST
{
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public class RestApiAttribute : Attribute
    {
        public string EndPoint { get; }

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
