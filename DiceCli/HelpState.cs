using System.Collections.Generic;
using System;

namespace DiceCli
{
    class HelpState : iState
    {
        public void RegisterMainPackageStates(iState package)
        {
            List<MainStates> states = new List<MainStates>()
            {
                MainStates.Help
            };
            StateManagerService.RegisterMainPackageStates(states, package);
        }

        public bool SetState(string line)
        {
            return false;
        }

        public bool Loop()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("");
            Console.WriteLine("Commands and Usages:");
            Console.WriteLine("");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("help - You are lookin at it.");
            Console.WriteLine("");
            Console.WriteLine("login - GetToken and Enter the server");
            Console.WriteLine("");
            Console.WriteLine("move - Move dice random positions");
            Console.WriteLine("");
            Console.WriteLine("exit");
            Console.ResetColor();

            StateManagerService.SetState(MainStates.Idle);
            return false;
        }
    }
}
