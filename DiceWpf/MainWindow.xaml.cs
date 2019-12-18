using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NLog;
using NLog.Config;
using Shared;

namespace DiceWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly List<string> _commandList = new List<string>();
        private const int MaxCommands = 50;
        private int _commandIndex = -1;

        private NetworkService _networkService;

        public MainWindow()
        {
            InitializeComponent();
            SetupNLog();
            consoleControlOutput.IsInputEnabled = false;

            //for (var i = 0; i < 5; ++i)
            //    consoleControlOutput.WriteOutput($"Hello, world {i}\n", Color.FromRgb(150, 100, 100));

            _networkService = new NetworkService();
            _networkService.Initialize();
        }

        private void OnKeyUpHandler(object sender, KeyEventArgs e)
        {
            if (_commandList.Count == 0)
                return;

            if (e.Key == Key.Up)
            {
                InputBox.Text = _commandList[_commandIndex];
                if (--_commandIndex <= 0)
                    _commandIndex = 0;
            }
            else if (e.Key == Key.Down)
            {
                InputBox.Text = _commandList[_commandIndex];
                var maxIndex = _commandList.Count - 1;
                if (++_commandIndex >= maxIndex)
                    _commandIndex = maxIndex;
            }
        }
        private void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && InputBox.Text != string.Empty)
            {
                string command = InputBox.Text;
                InputBox.Clear();
                if (command == "exit")
                {
                    this.Close();
                    return;
                }
                _networkService.EnqueueCommand(command);

                //consoleControlOutput.WriteOutput($"Input: {command}\n", Colors.White);
                Log.Debug(this, $"{command}");
                //Log.Warn(this, $"{command}");
                //Log.Error(this, $"{command}");
                //consoleControlInput.ClearOutput();

                _commandList.Add(command);
                if (_commandList.Count >= MaxCommands)
                    _commandList.RemoveAt(0);
                _commandIndex = _commandList.Count - 1;
            }
        }

        private void OnClose(object sender, EventArgs e)
        {
            _networkService.Deinitialize();
        }

        private void SetupNLog()
        {
            Log.EnsureInitialized();
            ConfigurationItemFactory.Default.Targets.RegisterDefinition("ConsoleControlLogTarget", typeof(ConsoleControlLogTarget));
            var config = new LoggingConfiguration();
            var consoleControlLogTarget = new ConsoleControlLogTarget(consoleControlOutput);
            config.AddTarget("consoleControlLogTarget", consoleControlLogTarget);
            config.AddRuleForAllLevels(consoleControlLogTarget);

            var logfile = new NLog.Targets.FileTarget("logfile") { FileName = "file.txt" };
            config.AddRuleForAllLevels(logfile);
            LogManager.Configuration = config;
        }
    }
}
