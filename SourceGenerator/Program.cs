using System;
using System.IO;

namespace SourceGenerator;

internal sealed class Program
{
    const int error_success = 0;
    const int error_fail = 1;

    static int Main(string[] args)
    {
        //System.Diagnostics.Debugger.Launch();

        try
        {
            App app = new App(args);
            return app.Run();
        }
        catch
        {
        }

        return error_fail;
    }

    private sealed class App
    {
        private readonly string path;

        public App(string[] args)
        {
            ArgumentOutOfRangeException.ThrowIfNotEqual(args.Length, 1, nameof(args));
            path = args[0];
        }

        public int Run()
        {
            File.WriteAllText(path, PostfixMapGenerator.Generate());   
            return error_success;
        }
    }
}
