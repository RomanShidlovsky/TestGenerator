using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Core.Collectors;
using Core.Extensions;

namespace Core
{
    public class TestGenerator
    {
        private SyntaxList<UsingDirectiveSyntax> _usings = new SyntaxList<UsingDirectiveSyntax>()
                .Add(UsingDirective(ParseName("System")))
                .Add(UsingDirective(ParseName("System.Collections.Generic")))
                .Add(UsingDirective(ParseName("System.Linq")))
                .Add(UsingDirective(ParseName("System.Text")))
                .Add(UsingDirective(ParseName("NUnit.Framework")))
                .Add(UsingDirective(ParseName("Moq")));
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

            var testMethods = GenerateTestMethods(classInfo);
            var setUp = GenerateSetUp(classInfo);
            

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
                                        SingletonList(
                                            AttributeList(
                                                SingletonSeparatedList(
                                                    Attribute(
                                                        IdentifierName("TestFixture"))))))
                                    .WithModifiers(
                                        TokenList(
                                            Token(SyntaxKind.PublicKeyword)))
                                    .WithMembers(new SyntaxList<MemberDeclarationSyntax>(setUp.Concat(testMethods)))))))
                    .NormalizeWhitespace().ToFullString());


        }

        private MemberDeclarationSyntax[] GenerateTestMethods(ClassInfo classInfo)
        {
            var classDeclaration = classInfo.ClassDeclaration;

            var methods = classDeclaration.ChildNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(node => node.Modifiers.Any(SyntaxKind.PublicKeyword))
                .ToList();


            methods.Sort((method1, method2) =>
                string.Compare(method1.Identifier.Text, method2.Identifier.Text, StringComparison.Ordinal));

            var testMethods = new MemberDeclarationSyntax[methods.Count];
            string prevName = "";
            int methodIndex = 0;

            for (int i = 0; i < methods.Count; i++)
            {
                // Generate name of identifier to call method
                var callClassName = methods[i].Modifiers.Any(SyntaxKind.StaticKeyword)
                    ? classInfo.FullName
                    : $"_{classDeclaration.Identifier.Text.ToCamelCase()}";
                
                // Generate method name
                var curName = methods[i].Identifier.Text;
                if (curName != prevName)
                {
                    curName += "Test";
                    methodIndex = 0;
                }
                else
                {
                    methodIndex++;
                    curName = $"{curName}Test{methodIndex}";
                }
                prevName = methods[i].Identifier.Text;

                // Generate arrange section
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

                // Assert.Fail("autogenerated");
                body.Add(ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("Assert"),
                        IdentifierName("Fail")))
                .WithArgumentList(
                    ArgumentList(
                        SingletonSeparatedList(
                            Argument(
                                LiteralExpression(
                                    SyntaxKind.StringLiteralExpression,
                                    Literal("autogenerated"))))))));

                


                testMethods[i] = MethodDeclaration(
                                    PredefinedType(
                                        Token(SyntaxKind.VoidKeyword)),
                                    Identifier(curName))
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

        private List<MemberDeclarationSyntax> GenerateSetUp(ClassInfo classInfo)
        {
            var classDeclaration = classInfo.ClassDeclaration;
            
            if (classDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                return new List<MemberDeclarationSyntax>();
            }

            var ctors = classDeclaration.ChildNodes()
                .OfType<ConstructorDeclarationSyntax>()
                .OrderBy(ctor => ctor.ParameterList.Parameters.Count)
                .ToList();

            var fields = new List<MemberDeclarationSyntax>();

            var classIdentifier = $"_{classDeclaration.Identifier.Text.ToCamelCase()}";
            // private <ClassName> _<className>;
            fields.Add(FieldDeclaration(
                                VariableDeclaration(
                                    IdentifierName(classInfo.FullName))
                                .WithVariables(
                                    SingletonSeparatedList(
                                        VariableDeclarator(
                                            Identifier(classIdentifier)))))
                            .WithModifiers(
                                TokenList(
                                    Token(SyntaxKind.PrivateKeyword))));

            var methodBody = new List<StatementSyntax>();
            var ctorArgs = new List<SyntaxNodeOrToken>();

            if (ctors.Count == 0)
            {
                methodBody.Add(ExpressionStatement(
                                        AssignmentExpression(
                                            SyntaxKind.SimpleAssignmentExpression,
                                            IdentifierName($"_{classDeclaration.Identifier.Text.ToCamelCase()}"),
                                            ObjectCreationExpression(
                                                IdentifierName(classInfo.FullName))
                                            .WithArgumentList(
                                                ArgumentList()))));
            }
            else
            {
                foreach (var parameter in ctors[0].ParameterList.Parameters)
                {
                    var typeName = parameter.Type!.ToString();
                    
                    if (typeName.StartsWith("I"))
                    {
                        var fieldName = $"_{parameter.Identifier.Text.ToCamelCase()}";

                        ctorArgs.Add(Argument(IdentifierName(fieldName)));

                        fields.Add(FieldDeclaration(
                                        VariableDeclaration(
                                            GenericName(
                                                Identifier("Mock"))
                                            .WithTypeArgumentList(
                                                TypeArgumentList(
                                                    SingletonSeparatedList<TypeSyntax>(
                                                        IdentifierName(typeName)))))
                                         .WithVariables(
                                            SingletonSeparatedList(
                                                VariableDeclarator(
                                                    Identifier(fieldName)))))
                                    .WithModifiers(
                                        TokenList(
                                            Token(SyntaxKind.PrivateKeyword))));

                        methodBody.Add(ExpressionStatement(
                                        AssignmentExpression(
                                            SyntaxKind.SimpleAssignmentExpression,
                                            IdentifierName(fieldName),
                                            ObjectCreationExpression(
                                                GenericName(
                                                    Identifier("Mock"))
                                                .WithTypeArgumentList(
                                                    TypeArgumentList(
                                                        SingletonSeparatedList<TypeSyntax>(
                                                            IdentifierName(typeName)))))
                                            .WithArgumentList(
                                                ArgumentList()))));
                    }
                    else
                    {
                        ctorArgs.Add(Argument(IdentifierName(parameter.Identifier.Text)));

                        methodBody.Add(LocalDeclarationStatement(
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

                    ctorArgs.Add(Token(SyntaxKind.CommaToken));
                }

                // Delete comma from the end of args list
                if (ctorArgs.Count != 0)
                {
                    ctorArgs.RemoveAt(ctorArgs.Count - 1);
                }

                methodBody.Add(ExpressionStatement(
                                        AssignmentExpression(
                                            SyntaxKind.SimpleAssignmentExpression,
                                            IdentifierName(classIdentifier),
                                            ObjectCreationExpression(
                                                IdentifierName(classInfo.FullName))
                                            .WithArgumentList(
                                                ArgumentList(
                                                    SeparatedList<ArgumentSyntax>(ctorArgs))))));
            }

            

            fields.Add(MethodDeclaration(
                                PredefinedType(
                                    Token(SyntaxKind.VoidKeyword)),
                                Identifier("SetUp"))
                            .WithAttributeLists(
                                SingletonList<AttributeListSyntax>(
                                    AttributeList(
                                        SingletonSeparatedList<AttributeSyntax>(
                                            Attribute(
                                                IdentifierName("SetUp"))))))
                            .WithModifiers(
                                TokenList(
                                    Token(SyntaxKind.PublicKeyword)))
                            .WithBody(
                                Block(methodBody)));

            return fields;
        }
    }
}