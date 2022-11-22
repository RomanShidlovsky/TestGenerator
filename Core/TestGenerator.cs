using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Core.Collectors;
using System.Reflection.Metadata;
using System.Xml.Linq;
using System.Data.Common;

namespace Core
{
    public class TestGenerator
    {
        private SyntaxList<UsingDirectiveSyntax> _usings = new SyntaxList<UsingDirectiveSyntax>()
                .Add(UsingDirective(ParseName("System")))
                .Add(UsingDirective(ParseName("System.Collections.Generic")))
                .Add(UsingDirective(ParseName("System.Linq")))
                .Add(UsingDirective(ParseName("System.Text")))
                .Add(UsingDirective(ParseName("NUnit.Framework")));
        public List<TestClassInfo> Generate(string source)
        {
            CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();

            ClassCollector collector = new ClassCollector();
            collector.Visit(root);

            var testClasses = collector.Classes.Select(GenerateTestClass).ToList();

            return testClasses;

        }

        private TestClassInfo GenerateTestClass(ClassInfo classInfo)
        {
            var classDeclaration = classInfo.ClassDeclaration;
            string @namespace = classInfo.Namespace;
            if (@namespace.IndexOf('.') != -1)
            {
                @namespace.Insert(@namespace.IndexOf('.') + 1, "Tests.");
            }
            else
            {
                @namespace += ".Tests";
            }


            return new TestClassInfo(classInfo.ClassDeclaration.Identifier.Text,
                CompilationUnit()
                    .WithUsings(new SyntaxList<UsingDirectiveSyntax>(_usings)
                        .Add(UsingDirective(ParseName(classInfo.Namespace))))
                    .WithMembers(
                        SingletonList<MemberDeclarationSyntax>(
                            NamespaceDeclaration(
                                ParseName(@namespace))
                            .WithMembers(
                                SingletonList<MemberDeclarationSyntax>(
                                    ClassDeclaration(classDeclaration.Identifier.Text + "Tests")
                                    .WithAttributeLists(
                                        SingletonList<AttributeListSyntax>(
                                            AttributeList(
                                                SingletonSeparatedList<AttributeSyntax>(
                                                    Attribute(
                                                        IdentifierName("TestFixture"))))))
                                    .WithModifiers(
                                        TokenList(
                                            Token(SyntaxKind.PublicKeyword)))
                                    .WithMembers(new SyntaxList<MemberDeclarationSyntax>(GenerateTestMethods(classDeclaration)))))))
                    .NormalizeWhitespace().ToFullString());


        }

        private MemberDeclarationSyntax[] GenerateTestMethods(ClassDeclarationSyntax classDeclaration)
        {
            var callClassName = classDeclaration.Identifier.Text;
            callClassName = "_" + Char.ToLowerInvariant(callClassName[0]) + callClassName.Substring(1);

            var methods = classDeclaration.ChildNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(node => node.Modifiers.Any(SyntaxKind.PublicKeyword))
                .ToList();


            methods.Sort((method1, method2) =>
                string.Compare(method1.Identifier.Text, method2.Identifier.Text, StringComparison.Ordinal));

            var testMethods = new MemberDeclarationSyntax[methods.Count];

            for (int i = 0; i < methods.Count; i++)
            {
                //Generate arrange section
                var body = new List<StatementSyntax>();
                var args = new List<SyntaxNodeOrToken>();
                foreach (var parameter in methods[i].ParameterList.Parameters)
                {                    
                    args.Add(Argument(IdentifierName(parameter.Identifier.Text)));
                    args.Add(Token(SyntaxKind.CommaToken));

                    // <ParameterType> <ParameterIdentifier> = default;
                    body.Add(LocalDeclarationStatement(
                                        VariableDeclaration(parameter.Type!)
                                        .WithVariables(
                                            SingletonSeparatedList(
                                                VariableDeclarator(
                                                    Identifier(parameter.Identifier.Text))
                                                .WithInitializer(
                                                    EqualsValueClause(
                                                        LiteralExpression(
                                                            SyntaxKind.DefaultLiteralExpression,
                                                            Token(SyntaxKind.DefaultKeyword))))))));
                }

                // Delete comma from the end of args list
                if (args.Count != 0)
                {
                    args.RemoveAt(args.Count - 1);
                }

                if (methods[i].ReturnType is PredefinedTypeSyntax typeSyntax
                    && typeSyntax.Keyword.ValueText == Token(SyntaxKind.VoidKeyword).ValueText)
                {
                    // Generate act section for method that return void
                    // _className.MethodName(args);
                    body.Add(ExpressionStatement(
                                InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName(callClassName),
                                        IdentifierName(methods[i].Identifier.Text)))
                                .WithArgumentList(
                                    ArgumentList(
                                        SeparatedList<ArgumentSyntax>(args)))));
                }
                else
                {
                    // Generate act section 
                    // var actual = MethodName(args);
                    body.Add(LocalDeclarationStatement(
                                VariableDeclaration(
                                    IdentifierName(
                                        Identifier(
                                            TriviaList(),
                                            SyntaxKind.VarKeyword,
                                            "var",
                                            "var",
                                            TriviaList())))
                             .WithVariables(
                                SingletonSeparatedList(
                                    VariableDeclarator(
                                        Identifier("actual"))
                                    .WithInitializer(
                                        EqualsValueClause(
                                            InvocationExpression(
                                                MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    IdentifierName(callClassName),
                                                    IdentifierName(methods[i].Identifier.Text)))
                                            .WithArgumentList(
                                                ArgumentList(
                                                    SeparatedList<ArgumentSyntax>(args)))))))));

                    // Generate assert section
                    // <MethodReturnType> expected = default;
                    body.Add(LocalDeclarationStatement(
                                        VariableDeclaration(methods[i].ReturnType)
                                        .WithVariables(
                                            SingletonSeparatedList(
                                                VariableDeclarator(
                                                    Identifier("expected"))
                                                .WithInitializer(
                                                    EqualsValueClause(
                                                        LiteralExpression(
                                                            SyntaxKind.DefaultLiteralExpression,
                                                            Token(SyntaxKind.DefaultKeyword))))))));

                    // Assert.That(actual, Is.EqualTo(expected));
                    body.Add(ExpressionStatement(
                                InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName(
                                            "Assert"),
                                        IdentifierName("That")))
                                .WithArgumentList(
                                    ArgumentList(
                                        SeparatedList<ArgumentSyntax>(
                                            new SyntaxNodeOrToken[]{
                                                Argument(
                                                    IdentifierName("actual")),
                                                Token(SyntaxKind.CommaToken),
                                                Argument(
                                                    InvocationExpression(
                                                        MemberAccessExpression(
                                                            SyntaxKind.SimpleMemberAccessExpression,
                                                            IdentifierName("Is"),
                                                            IdentifierName("EqualTo")))
                                                    .WithArgumentList(
                                                        ArgumentList(
                                                            SingletonSeparatedList<ArgumentSyntax>(
                                                                Argument(
                                                                    IdentifierName("expected"))))))})))));

                }


                testMethods[i] = MethodDeclaration(
                                    PredefinedType(
                                        Token(SyntaxKind.VoidKeyword)),
                                    Identifier(methods[i].Identifier.Text + "Test"))
                                 .WithAttributeLists(
                                    SingletonList(
                                        AttributeList(
                                            SingletonSeparatedList(
                                                Attribute(
                                                    IdentifierName("Test"))))))
                                 .WithModifiers(
                                    TokenList(
                                        Token(SyntaxKind.PublicKeyword)))
                                 .WithBody(Block(body));
            }
            return testMethods;
        }
    }
}