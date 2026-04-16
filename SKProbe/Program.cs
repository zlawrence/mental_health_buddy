using System;
using Microsoft.SemanticKernel;

class Program
{
    static void Main(string[] args)
    {
        var asm = typeof(Kernel).Assembly;
        Console.WriteLine(asm.FullName);
        foreach (var t in asm.GetTypes())
        {
            if (t.Namespace != null && (t.Namespace.StartsWith("Microsoft.SemanticKernel") || t.Namespace.StartsWith("Microsoft.")))
            {
                Console.WriteLine(t.FullName);
            }
        }
    }
}
