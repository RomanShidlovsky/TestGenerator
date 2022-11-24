using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Core
{
    public class ClassInfo
    {
        public ClassDeclarationSyntax ClassDeclaration { get; }
        public string Namespace { get; }
        public string FullName { get; } 
        public List<UsingDirectiveSyntax> Usings { get; }

        public ClassInfo(ClassDeclarationSyntax classDeclaration, string @namespace, string fullName, List<UsingDirectiveSyntax> usings)
        {
            ClassDeclaration = classDeclaration;
            Namespace = @namespace;
            FullName = fullName;
            Usings = usings;
        }
    }
}
