using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApexLogic.AutoREST
{
    /// <summary>
    /// Indicates that a method will not be made into a REST endpoint in a <see cref="RestApiAttribute"/>-marked object.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class RestIgnoreAttribute : Attribute
    {
    }
}
