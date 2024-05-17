using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApexLogic.AutoREST
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface, Inherited = false)]
    public class ResponseHeaderAttribute : Attribute
    {
		public string Header { get; }
		public string Value { get; }

		public ResponseHeaderAttribute(string id, string value)
		{
			Header = id;
			Value = value;
		}

	}
}
