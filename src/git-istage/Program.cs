using System;
using System.IO;

namespace GitIStage
{
    internal static class Program
    {
        private static void Main()
        {
            var repositoryPath = Directory.GetCurrentDirectory();

            var application = new Application();
            application.Run(repositoryPath);
        }
    }
}