using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ApexLogic.AutoREST.Utils;

namespace ApexLogic.AutoREST.CodeGeneration
{
    internal class StringFormatter
    {
        private StringBuilder _formatter;
        private Dictionary<string, string> _fields;

        public StringFormatter(string format) 
        {
            _formatter = new StringBuilder(format);
            _fields = new Dictionary<string, string>();
        }

        public void Set(string field, object value)
        {
            if(_fields.ContainsKey(field)) 
            {
                _fields[field] = value.ToString();
            }
            else
            {
                _fields.Add(field, value.ToString());
            }        
        }

        public override string ToString()
        {
            foreach(KeyValuePair<string, string> field in _fields)
            {
                _formatter.Replace('{' + field.Key + '}', field.Value);
            }
            return _formatter.ToString();
        }
    }
}
