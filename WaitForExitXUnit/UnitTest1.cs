using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Xunit;

namespace WaitForExitXUnit
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            var tempDir = GetTempDir();
            try
            {
                // Repro requires "dotnet publish" to create MSBuild NodeReuse child processes, which requires building multiple
                // projects in parallel.
                DotnetNew("classlib", Path.Combine(tempDir, "classlib1"));
                DotnetNew("classlib", Path.Combine(tempDir, "classlib2"));
                DotnetNew("web", Path.Combine(tempDir, "web"));
                DotnetAddReference(Path.Combine("..", "classlib1", "classlib1.csproj"), Path.Combine(tempDir, "web"));
                DotnetAddReference(Path.Combine("..", "classlib2", "classlib2.csproj"), Path.Combine(tempDir, "web"));

                Dotnet("publish", Path.Combine(tempDir, "web"));
            }
            finally
            {
                DeleteDir(tempDir);
            }
        }

        private static void DotnetNew(string template, string path)
        {
            Directory.CreateDirectory(path);
            Dotnet($"new {template} --no-restore", path);
        }

        private static void DotnetAddReference(string project, string path)
        {
            Dotnet($"add reference {project}", path);
        }

        private static void Dotnet(string arguments, string path)
        {
            var p = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "dotnet",
                    Arguments = arguments,
                    WorkingDirectory = path,
                    RedirectStandardOutput = true,
                }
            };
            p.EnableRaisingEvents = true;
            p.OutputDataReceived += (s, e) => Console.WriteLine(e.Data);

            StartAndWaitForExit(p);
        }

        private static void StartAndWaitForExit(Process process)
        {
            Console.WriteLine($"Before Start(): '{process.StartInfo.FileName} {process.StartInfo.Arguments}'");
            process.Start();
            Console.WriteLine($"Before BeginOutputReadLine(): '{process.StartInfo.FileName} {process.StartInfo.Arguments}'");
            process.BeginOutputReadLine();
            Console.WriteLine($"Before WaitForExit(): '{process.StartInfo.FileName} {process.StartInfo.Arguments}'");
            process.WaitForExit(int.MaxValue);
            Console.WriteLine($"After WaitForExit(int.MaxValue): '{process.StartInfo.FileName} {process.StartInfo.Arguments}'");
            process.WaitForExit();
            Console.WriteLine($"After WaitForExit(): '{process.StartInfo.FileName} {process.StartInfo.Arguments}'");
        }

        static string GetTempDir()
        {
            var temp = Path.GetTempFileName();
            File.Delete(temp);
            Directory.CreateDirectory(temp);
            return temp;
        }

        static void DeleteDir(string path)
        {
            for (var i = 0; i < 20; i++)
            {
                try
                {
                    var dir = new DirectoryInfo(path) { Attributes = FileAttributes.Normal };
                    foreach (var info in dir.GetFileSystemInfos("*", SearchOption.AllDirectories))
                    {
                        info.Attributes = FileAttributes.Normal;
                    }
                    dir.Delete(recursive: true);
                    break;
                }
                catch (DirectoryNotFoundException)
                {
                    break;
                }
                catch (FileNotFoundException)
                {
                    break;
                }
                catch (Exception)
                {
                    if (i < 19)
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(1));
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

    }
}
