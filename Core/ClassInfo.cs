using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public class ClassInfo
    {
        public ClassDeclarationSyntax ClassDeclaration { get; }
        public string Namespace { get; }
        public string FullName { get; } 

        public ClassInfo(ClassDeclarationSyntax classDeclaration, string @namespace, string fullName)
        {
            ClassDeclaration = classDeclaration;
            Namespace = @namespace;
            FullName = fullName;
        }
    }
}
