using SetupAppUtil.Logic;
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

            if (MainLogic.IsUpdater)
            {
                // WindowState = WindowState.Normal;
                Title = "Checking for Updates...";
            }
            else
            {
                WindowState = WindowState.Normal;
                Title = "Installing Program...";
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
