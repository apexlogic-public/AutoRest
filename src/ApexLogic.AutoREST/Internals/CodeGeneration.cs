using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApexLogic.AutoREST.Internals
{
    internal class CodeGeneration
    {
        public static NamespaceDeclarationSyntax CreateNamespace(string name, List<string> usings)
        {
            NamespaceDeclarationSyntax result = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(name));
            foreach (string usingNamespace in usings)
            {
                result = result.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(usingNamespace)));
            }
            return result;
        }

        public static ClassDeclarationSyntax CreateClass(string name, List<string> parents, List<string> modifiers)
        {
            ClassDeclarationSyntax result = SyntaxFactory.ClassDeclaration(name);
            foreach (string modifier in modifiers)
            {
                result = result.AddModifiers(SyntaxFactory.ParseToken(modifier));
            }
            foreach (string parent in parents)
            {
                result = result.AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(parent)));
            }

            return result;
        }


        public static string ToCode(MemberDeclarationSyntax syntax)
        {
            return syntax.ToFullString();
        }
    }
}
