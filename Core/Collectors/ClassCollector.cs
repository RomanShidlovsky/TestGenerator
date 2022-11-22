using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Collectors
{
    public class ClassCollector : CSharpSyntaxWalker
    {
        public ICollection<ClassInfo> Classes { get; } = new HashSet<ClassInfo>();

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            Classes.Add(new ClassInfo(node, GetClassNamespace(node), GetFullName(node)));
           
            base.VisitClassDeclaration(node);
        }

        private string GetClassNamespace(ClassDeclarationSyntax node)
        {
            StringBuilder builder = new();
            SyntaxNode current = node;

            while (current.Parent is NamespaceDeclarationSyntax || current.Parent is ClassDeclarationSyntax)
            {
                if (current.Parent is NamespaceDeclarationSyntax ns)
                {
                    builder.Insert(0, $"{ns.Name}.");
                }
                current = current.Parent;
            }
            if (builder.Length > 0)
            {
                builder.Remove(builder.Length - 1, 1);
            }
   


            return builder.ToString();
        }

        private string GetFullName(ClassDeclarationSyntax node)
        {
            StringBuilder builder = new();
            SyntaxNode current = node;

            builder.Append(node.Identifier.Text);

            while (current.Parent is ClassDeclarationSyntax parentClassDelcaration)
            {
                builder.Insert(0, $"{parentClassDelcaration.Identifier.Text}.");
                current = current.Parent;
            }

            return builder.ToString();
        }
    }
}
