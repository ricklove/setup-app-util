using SetupAppUtil.Logic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SetupAppUtil.WpfApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            WindowState = WindowState.Minimized;
            Hide();

            if (System.Environment.GetCommandLineArgs().Any(x => x.EndsWith("headless", System.StringComparison.InvariantCultureIgnoreCase)))
            {
               // Keep hidden
            }
            else if (MainLogic.IsUpdater)
            {
                Title = "Checking for Updates...";
                MainLogic.HasUpdateCallback = () => Dispatcher.Invoke(() =>
                {
                    Title = "Updating Program...";
                    WindowState = WindowState.Normal;
                    Show();
                });
            }
            else
            {
                WindowState = WindowState.Normal;
                Title = "Installing Program...";
                Show();
            }

            Run();
            // Loaded += (s, e) => Run();
        }

        private void Run()
        {
            Task.Run(() => StartProgressBar());
            Task.Run(async () =>
            {
                MainLogic.LogProvider = (message) => Dispatcher.Invoke(() => txtLog.Text += message + "\r\n");
                await MainLogic.Run();
                Dispatcher.Invoke(() => Close());
            });
        }

        private async Task StartProgressBar()
        {
            var remaining = 1.00;

            while (true)
            {
                remaining *= 0.8;

                Dispatcher.Invoke(() => ctlProgress.Value = (int)((1.0 - remaining) * 100));
                await Task.Delay(1000);
            }
        }
    }
}
