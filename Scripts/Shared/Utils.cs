using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Godot;

public static class Utils
{
    public static void PrintErr(string message)
    {
        var stackTrace = new StackTrace(true); // true to get file info
        var frame = stackTrace.GetFrame(0); // current frame
        int line = frame.GetFileLineNumber();
        string fullPath = frame.GetFileName();
        string fileName = Path.GetFileName(fullPath);
        string method = frame.GetMethod().Name;

        GD.PrintErr($"{message} [{fileName}:{line}->{method}()]");
    }

    public static void NullCheck(object obj, [CallerArgumentExpression("obj")] string paramName = null)
    {
        if (obj == null)
            PrintErr($"'{paramName}' is not set!");
    }
}
