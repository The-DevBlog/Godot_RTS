using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Godot;

public static class Utils
{
    public static void PrintErr(string message, StackFrame callerFrame = null)
    {
        var frame = callerFrame ?? new StackTrace(true).GetFrame(1);
        if (frame != null)
        {
            int line = frame.GetFileLineNumber();
            string fullPath = frame.GetFileName();
            string fileName = Path.GetFileName(fullPath);
            string method = frame.GetMethod().Name;

            GD.PrintErr($"{message} [{fileName}:{line}->{method}()]");
        }
        else
            GD.PrintErr(message);
    }

    public static void NullExportCheck(object obj, [CallerArgumentExpression("obj")] string paramName = null)
    {
        if (obj == null)
        {
            var frame = new StackTrace(true).GetFrame(1); // the actual caller of NullExportCheck
            PrintErr($"'{paramName}' is not set!", frame);
        }
    }

    public static void NullCheck(object obj, [CallerArgumentExpression("obj")] string paramName = null)
    {
        if (obj == null)
        {
            var frame = new StackTrace(true).GetFrame(1); // the actual caller of NullCheck
            PrintErr($"'{paramName}' is null", frame);
        }
    }

    public static void PrintTree(Node node, string indent = "")
    {
        GD.Print(indent + node.Name);
        foreach (Node child in node.GetChildren())
        {
            PrintTree(child, indent + "  ");
        }
    }
}
