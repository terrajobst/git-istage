            var isUndo = direction is PatchDirection.Reset or PatchDirection.Unstage;
                        newPatch.Append(',');
                        newPatch.Append(',');
                    newPatch.Append('\n');
                            newPatch.Append('\n');
                            newPatch.Append(' ');
                            newPatch.Append('\n');
            var isUndo = direction is PatchDirection.Reset or PatchDirection.Unstage;
            var patchLines = patch.Split('\n');
            
            // passing -v to git apply will output more useful information in case of a patch failure
            var arguments = $@"apply -v {cached} {reverse} --whitespace=nowarn ""{patchFilePath}""";
            File.WriteAllLines(patchFilePath, patchLines);
            
                void Handler(object _, DataReceivedEventArgs e)
                        if (e.Data != null) output.Add(e.Data);
                }
                process.OutputDataReceived += Handler;
                process.ErrorDataReceived += Handler;
                // show output only when an error occurred
                if (output.Any(l => l.Trim().Length > 0) && output.Any(q => q.StartsWith("error:")))
                        Console.WriteLine(line);
                    foreach (var line in patchLines)
                        Console.WriteLine(line);