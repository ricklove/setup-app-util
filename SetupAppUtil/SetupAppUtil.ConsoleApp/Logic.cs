using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SetupAppUtil.Logic
{
    public class MainLogic
    {
        private const string SETUP_CONFIG_NAME = "setup.config";
        private const string UPDATE_CONFIG_NAME = "update.config";
        private const string SETUP_EXE_NAME = "setup.exe";
        private const string UPDATE_EXE_NAME = "update.exe";

        public static Action<string> LogProvider = Console.WriteLine;
        private static void Log(string message) => LogProvider(message);

        public static bool IsUpdater => Process.GetCurrentProcess().MainModule.FileName.EndsWith(UPDATE_EXE_NAME, StringComparison.InvariantCultureIgnoreCase);

        public static async Task Run()
        {
            // Do update if needed
            var isUpdater = IsUpdater;
            if (isUpdater)
            {
                // Set Working Directory to act as setup
                var configFilePath = Path.Combine(Directory.GetCurrentDirectory(), UPDATE_CONFIG_NAME);
                Log($"Updating: ConfigFilePath={configFilePath}");

                var setupDir = File.ReadAllText(configFilePath);
                Log($"    setupDir={setupDir}");

                System.Environment.CurrentDirectory = setupDir;
            }

            var attempts = 0;
            while (attempts <= 2)
            {
                Log($"Installing...{(attempts > 0 ? $"attempt {attempts + 1}" : "")}");

                try
                {
                    await Task.Delay(1000 * attempts * attempts);
                    RunSetup(isUpdater);
                    break;
                }
                catch (Exception ex)
                {
                    Log($"{ex.Message}");
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
                    if (isUpdater
                        && File.GetLastWriteTimeUtc(destExeFilePath) >= File.GetLastWriteTimeUtc(sourceExeFilePath))
                    {
                        Log("Already Up to Date...");
                        return;
                    }

                    Log("Closing Running Program...");
                    var appExeImageName = Path.GetFileName(destExeFilePath);
                    var pInfo = new ProcessStartInfo("taskkill", $"/F /IM \"{appExeImageName}\"") { WindowStyle = ProcessWindowStyle.Hidden };
                    var killProcess = Process.Start(pInfo);
                    killProcess.WaitForExit();
                }

            }

            // Copy Files
            Log("Copying Files...");

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
                var setupExePath = Path.Combine(workingDir, SETUP_EXE_NAME);
                File.Copy(setupExePath, Path.Combine(destDir, UPDATE_EXE_NAME), true);
            }

            StartProgram(destDir);
        }

        private static void StartProgram(string destDir)
        {
            // Start Exe
            Log("Running...");
            var appExe = Directory.GetFiles(destDir, "*.exe").Where(x => !x.EndsWith(UPDATE_EXE_NAME)).First();
            Process.Start(appExe);

            Log("Done...");
        }
    }
}
