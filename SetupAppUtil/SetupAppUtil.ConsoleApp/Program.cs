using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SetupAppUtil.ConsoleApp
{
    internal class Program
    {
        private const string SETUP_CONFIG_NAME = "setup.config";
        private const string UPDATE_CONFIG_NAME = "update.config";
        private const string UPDATE_EXE_NAME = "update.exe";

        private static void Main(string[] args)
        {
            // Do update if needed
            var currentExePath = Process.GetCurrentProcess().MainModule.FileName;
            var isUpdater = currentExePath.EndsWith(UPDATE_EXE_NAME, StringComparison.InvariantCultureIgnoreCase);
            if (isUpdater)
            {
                // Set Working Directory to act as setup
                var configFilePath = Path.Combine(Directory.GetCurrentDirectory(), UPDATE_CONFIG_NAME);
                Console.WriteLine($"Updating: ConfigFilePath={configFilePath}");

                var setupDir = File.ReadAllText(configFilePath);
                Console.WriteLine($"    setupDir={setupDir}");

                System.Environment.CurrentDirectory = setupDir;
            }

            var attempts = 0;
            while (attempts <= 3)
            {
                Console.WriteLine($"Installing...{(attempts > 0 ? $"attempt {attempts + 1}" : "")}");

                try
                {
                    System.Threading.Thread.Sleep(1000 * attempts * attempts);
                    RunSetup(isUpdater);
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex.Message}");
                }

                attempts++;
            }
        }

        private static void RunSetup(bool isUpdater)
        {
            var destRootDir = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "Apps");
            var beforeRun = "";

            var configFilePath = SETUP_CONFIG_NAME;
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
                var proc = new ProcessStartInfo() { FileName = "cmd.exe", Arguments = "/c " + beforeRun, UseShellExecute = false };
                var p = Process.Start(proc);
                p.WaitForExit();
            }

            // Dest Dir
            var workingDir = Directory.GetCurrentDirectory();
            var sourceDir = Directory.GetDirectories(workingDir).First();
            var destDir = Path.Combine(destRootDir, new DirectoryInfo(sourceDir).Name);
            var sourceExeFilePath = Directory.GetFiles(sourceDir, "*.exe").First();

            // Task Kill (Warning: this will kill any task with a matching .exe filename)
            if (Directory.Exists(destDir))
            {
                var destExeFilePath = Path.Combine(destDir, Path.GetFileName(sourceExeFilePath));

                if (File.Exists(destExeFilePath))
                {
                    // Don't install if already updated
                    if (File.GetLastWriteTimeUtc(destExeFilePath) >= File.GetLastWriteTimeUtc(sourceExeFilePath))
                    {
                        Console.WriteLine("Already Up to Date...");
                        // System.Threading.Thread.Sleep(1000);
                        return;
                    }

                    var appExeImageName = Path.GetFileName(destExeFilePath);
                    var killProcess = Process.Start("taskkill", $"/F /IM \"{appExeImageName}\"");
                    killProcess.WaitForExit();
                }

            }

            // Copy Files
            Console.WriteLine("Copying Files...");

            var files = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);

            Directory.CreateDirectory(destDir);

            foreach (var file in files)
            {
                var destFile = file.Replace(sourceDir, destDir);
                Directory.CreateDirectory(Path.GetDirectoryName(destFile));
                File.Copy(file, destFile, true);
            }

            // Create Updater (Skip if already inside updater)
            if (!isUpdater)
            {
                File.WriteAllText(Path.Combine(destDir, UPDATE_CONFIG_NAME), workingDir);

                // Copy Setup as update.exe
                var setupExePath = Path.Combine(workingDir, "setup.exe");
                File.Copy(setupExePath, Path.Combine(destRootDir, UPDATE_EXE_NAME), true);
            }

            // Start Exe
            Console.WriteLine("Running...");
            var appExe = Directory.GetFiles(destDir, "*.exe").First();
            Process.Start(appExe);

            Console.WriteLine("Done...");
            System.Threading.Thread.Sleep(1000);
        }


        //private static void RunUpdate()
        //{
        //    try
        //    {
        //        var configFilePath = Path.Combine(Directory.GetCurrentDirectory(), UPDATE_CONFIG_NAME);
        //        if (!File.Exists(configFilePath)) { return; }

        //        var setupFilePath = File.ReadAllText(configFilePath);
        //        var setupDir = Path.GetDirectoryName(setupFilePath);

        //        // var configDoc = File.ReadAllText(configFilePath);
        //        //var sourceDir = configDoc;

        //        //if (!Directory.Exists(sourceDir)) { return false; }

        //        //var setupDir = Path.GetDirectoryName(sourceDir.TrimEnd('/', '\\'));
        //        //var setupFilePath = Path.Combine(setupDir, "setup.exe");

        //        //if (!File.Exists(setupFilePath)) { return false; }

        //        //// Check if current exe is older
        //        //var exeFilePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
        //        //var sourceExeFilePath = Path.Combine(sourceDir, Path.GetFileName(exeFilePath));

        //        //if (!File.Exists(sourceExeFilePath)) { return false; }
        //        //if (File.GetLastWriteTimeUtc(exeFilePath) >= File.GetLastWriteTimeUtc(sourceExeFilePath)) { return false; }

        //        // Run setup to Reinstall
        //        var processInfo = new System.Diagnostics.ProcessStartInfo(setupFilePath) { WorkingDirectory = setupDir, CreateNoWindow = false };
        //        System.Diagnostics.Process.Start(processInfo);
        //        return true;
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}
    }
}
