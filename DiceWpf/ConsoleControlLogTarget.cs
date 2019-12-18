using System.Windows.Media;
using NLog;
using NLog.Targets;

namespace DiceWpf
{
    [Target("ConsoleControlLogTarget")]
    public sealed class ConsoleControlLogTarget : TargetWithLayout  //or inherit from Target
    {
        private readonly ConsoleControl.WPF.ConsoleControl _consoleControl;
        public ConsoleControlLogTarget(ConsoleControl.WPF.ConsoleControl consoleControl)
        {
            _consoleControl = consoleControl;
        }
        protected override void Write(LogEventInfo logEvent)
        {
            var logMessage = this.Layout.Render(logEvent);
            logMessage += "\n";
            if (logEvent.Level == LogLevel.Warn)
                _consoleControl.WriteOutput(logMessage, Colors.Yellow);
            else if (logEvent.Level == LogLevel.Error)
                _consoleControl.WriteOutput(logMessage, Colors.Red);
            else
                _consoleControl.WriteOutput(logMessage, Colors.White);

            /*
            if (logEvent.Level == LogLevel.Warn)
                Debug.LogWarning(logMessage);
            else if (logEvent.Level == LogLevel.Error || logEvent.Level == LogLevel.Fatal)
                Debug.LogError(logMessage);
            else
                Debug.Log(logMessage);
            */
        }
    }
}
