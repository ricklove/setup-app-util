using System.IO;
using System.Linq;

namespace SetupAppUtil.ConsoleApp
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // Testing
            // Directory.SetCurrentDirectory(@"\\smiths.net\dfs\Work\Flex-Tek\STUT\Installs\TutcoTools\PdfMark");
            var destRootDir = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "Apps");

            var configFilePath = "setup.cfg";
            if (File.Exists(configFilePath))
            {
                var configDoc = File.ReadAllText(configFilePath);
                var configArgs = configDoc.Split('\n').Select(x => x.Trim()).ToList();
                destRootDir = configArgs[0];
            }

            // Copy Files
            var dir = Directory.GetDirectories(Directory.GetCurrentDirectory()).First();
            var destDir = Path.Combine(destRootDir, new DirectoryInfo(dir).Name);
            var files = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories);

            Directory.CreateDirectory(destDir);

            foreach (var file in files)
            {
                var destFile = file.Replace(dir, destDir);
                Directory.CreateDirectory(Path.GetDirectoryName(destFile));
                File.Copy(file, destFile, true);
            }

            // Start Exe
            var appExe = Directory.GetFiles(destDir, "*.exe").First();
            System.Diagnostics.Process.Start(appExe);
        }
    }
}
