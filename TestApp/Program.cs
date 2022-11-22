// See https://aka.ms/new-console-template for more information
using Core;

internal class Program
{
    private static void Main(string[] args)
    {
        TestGenerator testGenerator = new TestGenerator();
        string fileName = @"..\..\..\TestClass.cs";
        string path = @"..\..\..\..\..\out\";
        string text = File.ReadAllText(fileName);
        var classes = testGenerator.Generate(text);

        Console.WriteLine(GetDefaultValue(typeof(char)));
        
        
        foreach (var @class in classes)
        {
            File.WriteAllText(path + @class.Name + ".cs", @class.Code);
        }
    }

    public static string? GetDefaultValue(Type type)
    {
        if (type == typeof(string))
        {
            return "";
        }
        
        var value = type.IsValueType ? Activator.CreateInstance(type) : null;

        if (value == null)
        {
            return "null";
        }
        else
        {
            return value.ToString();
        }
    }
}