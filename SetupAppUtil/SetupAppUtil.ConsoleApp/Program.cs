using System;
using System.IO;
using System.Linq;

namespace SetupAppUtil.ConsoleApp
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var attempts = 0;
            while (attempts <= 3)
            {
                Console.WriteLine($"Installing...{(attempts > 0 ? $"attempt {attempts + 1}" : "")}");

                try
                {
                    System.Threading.Thread.Sleep(1000 * attempts * attempts);
                    RunSetup();
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex.Message}");
                }

                attempts++;
            }
        }

        private static void RunSetup()
        {
            var destRootDir = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "Apps");
            var beforeRun = "";

            var configFilePath = "setup.config";
            if (File.Exists(configFilePath))
            {
                var configDoc = File.ReadAllText(configFilePath);
                var configArgs = configDoc.Split('\n').Select(x => x.Trim()).ToList();
                destRootDir = configArgs.Where(x => x.StartsWith("DEST=")).Select(x => x.Substring(5).Trim()).FirstOrDefault() ?? destRootDir;
                beforeRun = configArgs.Where(x => x.StartsWith("BEFORE=")).Select(x => x.Substring(7).Trim()).FirstOrDefault() ?? destRootDir;
            }

            // Before run
            if (!string.IsNullOrEmpty(beforeRun))
            {
                var proc = new System.Diagnostics.ProcessStartInfo() { FileName = "cmd.exe", Arguments = "/c " + beforeRun, UseShellExecute = false };
                var p = System.Diagnostics.Process.Start(proc);
                p.WaitForExit();
            }

            // Dest Dir
            var dir = Directory.GetDirectories(Directory.GetCurrentDirectory()).First();
            var destDir = Path.Combine(destRootDir, new DirectoryInfo(dir).Name);

            // Move Existing Folder
            Console.WriteLine("Removing Existing Version...");

            if (Directory.Exists(destDir))
            {
                // Try to move existing to temp folder (where it can be auto deleted by OS)
                var tempPathDir = Path.Combine(Path.GetTempPath(), new DirectoryInfo(dir).Name);
                Directory.Move(destRootDir, tempPathDir);
            }

            // Copy Files
            Console.WriteLine("Copying Files...");

            var files = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories);

            Directory.CreateDirectory(destDir);

            foreach (var file in files)
            {
                var destFile = file.Replace(dir, destDir);
                Directory.CreateDirectory(Path.GetDirectoryName(destFile));
                File.Copy(file, destFile, true);
            }

            File.WriteAllText(Path.Combine(destDir, "setup.config"), dir);

            // Start Exe
            Console.WriteLine("Running...");
            var appExe = Directory.GetFiles(destDir, "*.exe").First();
            System.Diagnostics.Process.Start(appExe);

            System.Threading.Thread.Sleep(3000);
            Console.WriteLine("Done...");
        }
    }
}
