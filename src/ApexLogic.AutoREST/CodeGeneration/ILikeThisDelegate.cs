using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApexLogic.AutoREST
{
    public interface ILikeThisDelegate
    {
        Func<string, Type, Dictionary<string, object>, object> _methodDelegate { get; set; }
        Func<string, ServerSideEvent> _eventDelegate { get; set; }
    }
}
