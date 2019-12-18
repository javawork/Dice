using System;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            var apiUrl = System.Configuration.ConfigurationManager.AppSettings["apiUrl"];
            var server = new Server(apiUrl);
            server.Start();

            do
            {
                try
                {
                    server.Update();
                }
                catch (Exception e)
                {
                    Log.Error(typeof(Program), $"Exception while update.");
                    Log.Error(typeof(Program), $"{e.ToString()}");
                }

            } while (!AnyShutdownSignal());
            Log.Debug(typeof(Program), "Terminating Server");
            server.Terminate();
        }

        private static bool AnyShutdownSignal()
        {
            if (!Console.KeyAvailable)
                return false;

            var inputInfo = Console.ReadKey();
            return inputInfo.Key == ConsoleKey.Escape;
        }
    }
}
