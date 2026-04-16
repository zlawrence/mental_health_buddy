using System;
using System.Linq;
using System.Reflection;
using Microsoft.SemanticKernel.ChatCompletion;

class Program
{
    static void Main()
    {
        var type = typeof(ChatCompletionServiceExtensions);
        Console.WriteLine(type.FullName);
        foreach (var m in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            Console.WriteLine(m);
        }
    }
}
