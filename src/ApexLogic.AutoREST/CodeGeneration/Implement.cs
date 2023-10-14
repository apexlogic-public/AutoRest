using ApexLogic.AutoREST.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ApexLogic.AutoREST
{
    /// <summary>
    /// Utility class to generate flexible clients for use with a <see cref="RestApiServer"/> service.
    /// </summary>
    /// <typeparam name="T">The interaface adorned with a <see cref="RestApiAttribute"/> attribute that acts as a service descriptor.</typeparam>
    public class Implement<T>
    {
        private const string CLASS_SCAFFOLDING =
            "{4}\n" +
            "public class {1} : {2}, ILikeThisDelegate\n" +
            "{{\n" +
            "    {3}\n" +
            "}}\n\n" +
            "return new {1}();";
        private const string METHOD_SCAFFOLDING = "public {0} {1}({2})\n {{\n {3}\n }}";

        private const string VOID_CALL_SCAFFOLDING = "_methodDelegate(\"{0}\", null, {1});";
        private const string RETURN_DANYMIC_CALL_SCAFFOLDING = "return _methodDelegate(\"{1}\", typeof({0}), {2});";
        private const string RETURN_CALL_SCAFFOLDING = "return ({0})Convert.ChangeType(_methodDelegate(\"{1}\", typeof({0}), {2}), typeof({0}));";
        private const string EVENT_SCAFFOLDING = "public ServerSideEvent {0} {{ get {{ if(_{0} == null) {{ _{0} = _eventDelegate(\"{0}\"); }} return _{0}; }} }} private ServerSideEvent _{0};";

        private const string METHOD_DELEGATE_PROP = "public Func<string, Type, Dictionary<string, object>, object> _methodDelegate { get; set; }";
        private const string EVENT_DELEGATE_PROP = "public Func<string, ServerSideEvent> _eventDelegate { get; set; }";

        /// <summary>
        /// Generates a dynamic class that implements <typeparamref name="T"/> with method bodies defines in <paramref name="methodPredicate"/>.
        /// </summary>
        /// <param name="methodPredicate">The method "implementation" delegate.</param>
        /// <param name="eventCreator">A delegate to create <see cref="ServerSideEvent"/> objects to use on the client-side (See <see cref="ServerSideEventClient"/> for a reference implementation).</param>
        /// <returns>A danamic class implementing the interface <typeparamref name="T"/>.</returns>
        public static T LikeThis(Func<string, Type, Dictionary<string, object>, object> methodPredicate, Func<string, ServerSideEvent> eventCreator)
        {
            List<SourceWithUsings> methods = new List<SourceWithUsings>();
            
            SourceWithUsings libUsings = new SourceWithUsings();
            libUsings.Usings.Add(typeof(T).Namespace);
            libUsings.Usings.Add("System");
            libUsings.Usings.Add("ApexLogic.AutoREST");
            libUsings.Usings.Add("System.Collections.Generic");
            libUsings.Assemblies.Add(typeof(object).Assembly);
            libUsings.Assemblies.Add(typeof(T).Assembly);
            methods.Add(libUsings);

            foreach (MethodInfo method in typeof(T).GetMethods())
            {
                if(method.IsSpecialName)
                {
                    continue;
                }

                string body = "";
                if (method.ReturnType == typeof(void))
                {
                    body = string.Format(VOID_CALL_SCAFFOLDING,
                                            method.Name, 
                                            CreateDynamicMethodParameterDictionary(method)
                                        );
                }
                else if (method.ReturnType == typeof(object))
                {
                    body = string.Format(RETURN_DANYMIC_CALL_SCAFFOLDING,
                                            CreateTypeSourceString(method.ReturnType).SourceCode, 
                                            method.Name, 
                                            CreateDynamicMethodParameterDictionary(method)
                                        );
                }
                else
                {
                    body = string.Format(RETURN_CALL_SCAFFOLDING,
                                    CreateTypeSourceString(method.ReturnType).SourceCode, 
                                    method.Name, 
                                    CreateDynamicMethodParameterDictionary(method)
                                );
                }
                methods.Add(CreateDynamicMethodSource(method, body));
            }

            foreach (PropertyInfo property in typeof(T).GetProperties())
            {

                if (property.IsSpecialName)
                {
                    continue;
                }

                string body = "";
                if (property.PropertyType == typeof(ServerSideEvent))
                {
                    body = string.Format(EVENT_SCAFFOLDING, property.Name);
                }
                methods.Add(new SourceWithUsings() { SourceCode = body });
            }

            Tuple<string, string> generated = CreateDynamicTypeSource(methods.SelectMany(m => m.Usings).Distinct().ToList());
            string typeName = generated.Item1;
            string scaffold = generated.Item2;

            scaffold = scaffold.Replace("{3}", METHOD_DELEGATE_PROP + "\n" + EVENT_DELEGATE_PROP + "\n\n" + string.Join("\n\n", methods.Select(m => m.SourceCode)));

            ScriptOptions options = ScriptOptions.Default;
            Assembly[] assemblies = methods.SelectMany(m => m.Assemblies).Distinct().ToArray();
            options = options.AddReferences(assemblies);

            Script script = CSharpScript.Create<T>(scaffold, options);
            Task<ScriptState> run = script.RunAsync();
            run.Wait();
            ILikeThisDelegate result = (ILikeThisDelegate)run.Result.ReturnValue;
            result._methodDelegate = methodPredicate;
            result._eventDelegate = eventCreator;

            return (T)result;
        }

        private static Tuple<string, string> CreateDynamicTypeSource(List<string> usings)
        {
            string dynamicNamespace = $"ApexLogic.Libs.DynamicCode._Dynamic_{new Random().Next()}";
            string implementetClassName = "ImplementedType";

            usings.Add(typeof(T).Namespace);

            string source = string.Format(CLASS_SCAFFOLDING, 
                                            dynamicNamespace, 
                                            implementetClassName, 
                                            typeof(T).Name, 
                                            "{3}", 
                                            string.Join("\r\n", usings.Select(u => $"using {u};"))
                                        );
            return new Tuple<string, string>($"{dynamicNamespace}.{implementetClassName}", source);
        }

        private static SourceWithUsings CreateDynamicMethodSource(MethodInfo info, string body)
        {
            SourceWithUsings result = new SourceWithUsings();

            List<string> parameters = new List<string>();
            foreach (ParameterInfo parameter in info.GetParameters())
            {
                SourceWithUsings paramTypeSource = CreateTypeSourceString(parameter.ParameterType);
                parameters.Add($"{paramTypeSource.SourceCode} {parameter.Name}");
                result.Combine(paramTypeSource);
            }

            SourceWithUsings returnType = CreateTypeSourceString(info.ReturnType);
            result.Combine(returnType);
            result.SourceCode = string.Format(METHOD_SCAFFOLDING, returnType.SourceCode, info.Name, string.Join(", ", parameters), body);

            return result;
        }

        private static string CreateDynamicMethodParameterDictionary(MethodInfo info)
        {
            List<string> parameters = new List<string>();
            foreach (ParameterInfo parameter in info.GetParameters())
            {
                parameters.Add($"{{ \"{parameter.Name}\", {parameter.Name} }}");
            }
            return "new Dictionary<string, object>(){ " + string.Join(", ", parameters) + "}";

        }

        private static SourceWithUsings CreateTypeSourceString(Type t)
        {
            SourceWithUsings result = new SourceWithUsings();

            if (t.IsGenericType)
            {
                List<SourceWithUsings> genericParts = t.GenericTypeArguments.Select(g => CreateTypeSourceString(g)).ToList();
                result.Combine(genericParts);
                result.SourceCode = t.Name.Substring(0, t.Name.IndexOf('`')) + "<" + string.Join(", ", genericParts.Select(p => p.SourceCode)) + ">";
            }
            else
            {
                if (t.Name == "Void")
                {
                    result.SourceCode = "void";
                }
                else
                {
                    result.SourceCode = t.Name;
                    result.AppendType(t);
                }
            }

            return result;
        }
    }
}
