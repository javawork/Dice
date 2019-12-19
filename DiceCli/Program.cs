using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiceCli
{
    class Program
    {
        private static iState _currentPackage;
        private static bool _isTypedTextHidden = false;
        private static string _line;
        private static StringBuilder _lineSb;
        private static bool _argsRead;
        private static string[] _args = new string[0];
        private static bool _readLine = false;
        private static bool _isCLI = false;
        private static NetworkService _networkService;

        static void Main(string[] args)
        {
            _args = args;
            _lineSb = new StringBuilder();

            //Parse Args and determine if this is a CLI or Console mode.
            if (args.Length > 0 && !_argsRead)
            {
                _isCLI = true;
                foreach (var a in args)
                {
                    _lineSb.Append(a + " ");
                }
                _line = _lineSb.ToString();
                _argsRead = true;
                _args = new string[0];
            }
            else
            {
                _readLine = true;
                _argsRead = true;
            }

            Console.WriteLine("");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("={ ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Dice CLI");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(" }=");
            Console.ForegroundColor = ConsoleColor.White;

            //if this is a console app then we want to show them how to get help.
            if (!_isCLI)
            {
                Console.WriteLine("");
                Console.WriteLine("Type: 'help' for a list of commands");
                Console.WriteLine("");
                Console.Write(">");
            }
            else
            {
                Console.WriteLine("");
                Console.WriteLine("");
            }

            _networkService = new NetworkService();
            _networkService.Initialize();

            var apiUrl = System.Configuration.ConfigurationManager.AppSettings["apiUrl"];

            var consolePackage = new HelpState();
            consolePackage.RegisterMainPackageStates(consolePackage);
            var mainLoop = new MainLoopState();
            mainLoop.RegisterMainPackageStates(mainLoop);
            var login = new LoginState(_networkService, apiUrl);
            login.RegisterMainPackageStates(login);
            var autoMove = new AutoMoveState(_networkService);
            autoMove.RegisterMainPackageStates(autoMove);

            do
            {
                //if we are a console app, read the command that is entered.
                if (_args.Length == 0 && _readLine)
                {
                    if (!_isTypedTextHidden)
                    {
                        //Read the line input from the console.
                        _line = Console.ReadLine();
                    }
                    else
                    {
                        //Read the line in a different way.
                        ConsoleKeyInfo key;
                        do
                        {
                            key = Console.ReadKey(true);
                            if (key.Key != ConsoleKey.Enter)
                            {
                                var s = string.Format("{0}", key.KeyChar);
                                _lineSb.Append(s);
                            }
                        } while (key.Key != ConsoleKey.Enter);
                        _line = _lineSb.ToString();
                    }
                }

                //Set read line to true, not it will only be false if we came from a CLI.
                _readLine = true;
                var loopReturn = false;
                if (StateManagerService.IsIdle())
                {
                    //If we are idle then we want to check for commands.
                    StateManagerService.SetState(_line);
                    _currentPackage = StateManagerService.GetPackage();
                    _isTypedTextHidden = _currentPackage.SetState(_line);
                    loopReturn = _currentPackage.Loop();
                }
                else
                {
                    //If we are not idle, then we want to process the _line for arguments.

                    //get the correct package for the state we are in
                    _currentPackage = StateManagerService.GetPackage();

                    //process the package state
                    _isTypedTextHidden = _currentPackage.SetState(_line);

                    //do package loop, which contains logic to do stuff.
                    loopReturn = _currentPackage.Loop();
                }

                //if this is a CLI then we just want to exit.
                if (!_isCLI)
                {
                    //Prompt or exit.
                    if (!loopReturn)
                    {
                        Console.Write(">");
                    }
                    else
                    {
                        _line = null;
                    }
                }
                else
                {
                    _line = null;
                }

            } while (_line != null);

            _networkService.Deinitialize();
        }
    }
}
