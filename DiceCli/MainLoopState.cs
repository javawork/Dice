using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiceCli
{
    public class MainLoopState : iState
    {
        public enum States
        {
            None,
            Exit
        }

        private static States _state = States.None;

        public void RegisterMainPackageStates(iState package)
        {
            List<MainStates> states = new List<MainStates>()
            {
                MainStates.Idle,
                MainStates.Exit
            };
            StateManagerService.RegisterMainPackageStates(states, package);
        }

        public bool SetState(string line)
        {
            var lline = line.ToLower();

            if (lline.Contains("exit"))
            {
                _state = States.Exit;
            }
            return false;
        }

        public bool Loop()
        {
            if (_state != States.Exit)
            {
                return false;
            }
            _state = States.None;
            StateManagerService.SetState(MainStates.Idle);

            //Returning true will exit the program.
            return true;
        }
    }
}
