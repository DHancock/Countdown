using System;
using System.IO;

namespace SourceGenerator;

internal sealed class Program
{
    const int error_success = 0;
    const int error_fail = 1;

    static int Main(string[] args)
    {
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
        private readonly string destinationPath;

        public App(string[] args)
        {
            if (args.Length != 1)
                throw new ArgumentOutOfRangeException(nameof(args));

            destinationPath = args[0];

            if (!Directory.Exists(Path.GetDirectoryName(destinationPath)))
                throw new DirectoryNotFoundException();
        }

        public int Run()
        {
            File.WriteAllText(destinationPath, PostfixMapGenerator.Generate());   
            return error_success;
        }
    }
}
