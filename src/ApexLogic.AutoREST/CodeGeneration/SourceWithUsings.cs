using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ApexLogic.AutoREST.CodeGeneration
{
    internal class SourceWithUsings
    {
        public string SourceCode { get; set; }
        public List<string> Usings { get; } = new List<string>();
        public List<Assembly> Assemblies { get; } = new List<Assembly>();

        public void AppendType(Type t)
        {
            Assemblies.Add(t.Assembly);
            Usings.Add(t.Namespace);
        }

        public void Combine(SourceWithUsings other)
        {
            Usings.AddRange(other.Usings);
            Assemblies.AddRange(other.Assemblies);
        }

        public void Combine(IEnumerable<SourceWithUsings> other)
        {
            Usings.AddRange(other.SelectMany(s => s.Usings));
            Assemblies.AddRange(other.SelectMany(s => s.Assemblies));
        }
    }
}
