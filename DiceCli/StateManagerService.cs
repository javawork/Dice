using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiceCli
{
    public enum MainStates
    {
        Idle,
        Exit,
        Help,
        Login,
        AutoMove,
    }

    public class StateManagerService
    {
        public static Dictionary<MainStates, iState> PackageCache = new Dictionary<MainStates, iState>();
        private static MainStates _state = MainStates.Idle;
        private static bool validCommand = false;

        public static void RegisterMainPackageStates(List<MainStates> states, iState package)
        {
            foreach (var state in states)
            {
                PackageCache.Add(state, package);
            }
        }

        public static void SetState(MainStates state)
        {
            _state = state;
        }

        public static iState GetPackage()
        {
            return PackageCache[_state];
        }

        public static bool IsIdle()
        {
            return _state == MainStates.Idle;
        }

        public static bool SetState(string line)
        {
            var lline = line.ToLower();

            if (lline.Contains("help") || lline.Contains("?"))
            {
                validCommand = true;
                _state = MainStates.Help;
            }

            if (lline.Contains("login"))
            {
                validCommand = true;
                _state = MainStates.Login;
            }

            if (lline.Contains("move"))
            {
                validCommand = true;
                _state = MainStates.AutoMove;
            }

            if (lline.Contains("exit"))
            {
                _state = MainStates.Exit;
            }
            return false;
        }


    }
}
