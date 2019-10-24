using System.Threading.Tasks;

namespace SetupAppUtil.ConsoleApp
{
    internal class Program
    {
        private static async Task Main(string[] args) => await Logic.MainLogic.Run();
    }
}
