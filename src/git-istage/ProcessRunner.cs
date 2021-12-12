using System.Collections.Generic;
using System.Diagnostics;

namespace GitIStage
{
    internal interface IProcessRunner
    {
        IEnumerable<string> Run(string fileName, string arguments);
        IEnumerable<string> Run(string fileName, string arguments, string workingDirectory, bool redirectStandardStreams);
    }

    internal class ProcessRunner : IProcessRunner
    {
        public IEnumerable<string> Run(string fileName, string arguments)
        {
            return Run(fileName, arguments, null, false);
        }

        public IEnumerable<string> Run(string fileName, string arguments, string workingDirectory, bool redirectStandardStreams = false)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                WorkingDirectory = workingDirectory,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            if (redirectStandardStreams)
            {
                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardOutput = true;
            }

            List<string> result = new List<string>();

            void OnDataReceived(object sender, DataReceivedEventArgs e)
            {
                lock (result)
                {
                    string line = e.Data;
                    if (line == null)
                    {
                        return;
                    }

                    result.Add(line);
                }
            }

            using (var process = new Process())
            {
                process.StartInfo = startInfo;

                if (redirectStandardStreams)
                {
                    process.OutputDataReceived += OnDataReceived;
                    process.ErrorDataReceived += OnDataReceived;
                }

                process.Start();

                if (redirectStandardStreams)
                {
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                }

                process.WaitForExit();
            }

            return result;
        }
    }
}
