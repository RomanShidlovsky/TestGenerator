using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Core.Tests
{
    public class TestsGeneratorTests
    {
        private readonly TestsGenerator _generator = new TestsGenerator();

        [Test]
        public void TestClassesCountTest()
        {
            var testClasses = _generator.Generate(_programText1 + _programText2);
            Assert.That(testClasses.Count, Is.EqualTo(5));

            foreach (var testClass in testClasses)
            {
                var compUnitRoot = CSharpSyntaxTree.ParseText(testClass.Code).GetCompilationUnitRoot();
                Assert.That(compUnitRoot.DescendantNodes().OfType<ClassDeclarationSyntax>().Count(), Is.EqualTo(1));
            }
        }

        [Test]
        public void MethodsCountTest()
        {
            // Including SetUp method
            var expectedCount = new int[3] {2, 4, 2};
            
            var testClasses = _generator.Generate(_programText1);
            Assert.That(testClasses.Count, Is.EqualTo(3));

            for (int i = 0; i < testClasses.Count; i++)
            {
                var compUnitRoot = CSharpSyntaxTree.ParseText(testClasses[i].Code).GetCompilationUnitRoot();
                var actualCount = compUnitRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().Count();
                Assert.That(actualCount, Is.EqualTo(expectedCount[i]));
            }
        }

        [Test]
        public void OverloadedMethodsTest()
        {
            var testClasses = _generator.Generate(_programText1);
            Assert.That(testClasses.Count, Is.EqualTo(3));

            var compUnitRoot = CSharpSyntaxTree.ParseText(testClasses[1].Code).GetCompilationUnitRoot();
            var methods = compUnitRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();

            Assert.That(methods.Count, Is.EqualTo(4));

            Assert.Multiple(() =>
            {
                Assert.That(methods[1].Identifier.Text, Is.EqualTo("StartTest"));
                Assert.That(methods[2].Identifier.Text, Is.EqualTo("StartTest1"));
            });
        }

        [Test]
        public void FileScopedNamespaceTest()
        {
            var testClasses = _generator.Generate(_programText2);
            var compUnitRoot = CSharpSyntaxTree.ParseText(testClasses[0].Code).GetCompilationUnitRoot();
            var usings = compUnitRoot.DescendantNodes().OfType<UsingDirectiveSyntax>().ToList();
            var usingFromNamespace = usings[usings.Count - 1].Name.ToString();

            Assert.That(usingFromNamespace, Is.EqualTo("Test2"));
        }

        [Test]
        public void SetUpMethodTest()
        {
            var testClasses = _generator.Generate(_programText1);
            Assert.That(testClasses, Has.Count.EqualTo(3));

            var compUnitRoot = CSharpSyntaxTree.ParseText(testClasses[1].Code).GetCompilationUnitRoot();
            var setUpMethod = compUnitRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().First();
            Assert.Multiple(() => {
                Assert.That(setUpMethod.Identifier.Text, Is.EqualTo("SetUp"));
                Assert.That(setUpMethod.AttributeLists[0].Attributes.First().Name.ToString(), Is.EqualTo("SetUp"));
            });

            var fieldDeclarations = compUnitRoot.DescendantNodes().OfType<FieldDeclarationSyntax>().ToList();
            Assert.That(fieldDeclarations, Has.Count.EqualTo(2));

            var methodBody = setUpMethod.Body;
            Assert.That(methodBody, Is.Not.Null);
            var statements = methodBody!.Statements;
            Assert.That(statements, Has.Count.EqualTo(2));

            Assert.Multiple(() =>
            {
                Assert.That(statements[0].ToString(), Is.EqualTo("_reader = new Mock<IDataReader>();"));
                Assert.That(statements[1].ToString(), Is.EqualTo("_innerClass = new Test1.InnerClass(_reader.Object);"));
            });
        }

        [Test]
        public void StaticMethodTest()
        {
            var testClasses = _generator.Generate(_programText1);
            var compUnitRoot = CSharpSyntaxTree.ParseText(testClasses[2].Code).GetCompilationUnitRoot();
            var staticMethodTest = compUnitRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().ToArray()[1];
            Assert.That(staticMethodTest, Is.Not.Null);

            var staticMethodCall = staticMethodTest.Body!.DescendantNodes().OfType<StatementSyntax>().ToArray()[1];
            Assert.That(staticMethodCall, Is.Not.Null);

            Assert.That(staticMethodCall.ToString(), Is.EqualTo("InInnerNamespaceClass.StaticMethod(c);"));
        }

        [Test]
        public void StaticClassTest()
        {
            var testClasses = _generator.Generate(_programText2);
            var compUnitRoot = CSharpSyntaxTree.ParseText(testClasses[1].Code).GetCompilationUnitRoot();
            var methods = compUnitRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().ToArray();

            foreach (var method in methods)
            {
                Assert.That(method.Identifier.Text, Is.Not.EqualTo("SetUp"));
            }

            var staticMethodStatement = methods.First().Body!.DescendantNodes().OfType<StatementSyntax>().First();
            Assert.That(staticMethodStatement.ToString(), Is.EqualTo("StaticClass.StaticMethod();"));
        }

        private const string _programText1 = @"
            using System;
            using System.Collections.Generic;
            using System.Data;
            using System.Linq;
            using System.Text;
            using System.Threading.Tasks;

            namespace TestClasses
            {
                public class Test1
                {
                    public class InnerClass
                    {
                        private IDataReader _dataReader;
                        public InnerClass(IDataReader reader)
                        {
                            _dataReader = reader;
                        }

                        public void Start(int a) { }

                        public int Start(int a, double b) 
                        { 
                            return 0; 
                        }

                        public bool Stop(int a) { return false; }
                    }

                    public int P { get; set; }

                    public Test1(int p)
                    {
                        P = p;
                    }

                    public int GetValue(object obj) { return P; }
                }

                namespace InnerNamespace
                {
                    public class InInnerNamespaceClass
                    {
                        public static void StaticMethod(char c) { }
                    }
                }
            }";

        private const string _programText2 = @"
            using System.Data;

            namespace Test2;

            public class FileScopedNamespaceTest
            {
                public object GetObject(DataColumn dataColumn)
                {
                    return new object();
                }

                private void PrivateMethod() { }
                internal void InternalMethod() { }
                protected void ProtectedMethod() { }
            }

            public static class StaticClass
            {
                public static void StaticMethod() { }
            }";
    }
}