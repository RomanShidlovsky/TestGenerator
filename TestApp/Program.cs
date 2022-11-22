// See https://aka.ms/new-console-template for more information
using Core;

internal class Program
{
    private static void Main(string[] args)
    {
        TestGenerator testGenerator = new TestGenerator();
        string fileName = @"..\..\..\Class1.cs";
        string path = @"..\..\..\..\..\out\";
        string text = File.ReadAllText(fileName);
        var classes = testGenerator.Generate(text);

        foreach (var @class in classes)
        {
            File.WriteAllText(path + @class.Name + ".cs", @class.Code);
        }
    }
}