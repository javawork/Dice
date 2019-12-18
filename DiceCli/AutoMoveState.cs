using System.Collections.Generic;
using System;
using Shared;
using System.Threading;

namespace DiceCli
{
    class AutoMoveState : iState
    {
        private readonly NetworkService _netService;
        public void RegisterMainPackageStates(iState package)
        {
            List<MainStates> states = new List<MainStates>()
            {
                MainStates.AutoMove
            };
            StateManagerService.RegisterMainPackageStates(states, package);
        }

        public AutoMoveState(NetworkService service)
        {
            _netService = service;
        }

        public bool SetState(string line)
        {
            return false;
        }

        public bool Loop()
        {
            Console.WriteLine("AutoMoveState");
            var rnd = new Random(Guid.NewGuid().GetHashCode());
            for (var i = 0; i < 5; ++i)
            {
                var pos = SharedUtil.GetRandomPosition(rnd);
                _netService.EnqueueCommand($"move {pos.X} {pos.Y}");
                Thread.Sleep(1000);
            }

            StateManagerService.SetState(MainStates.Idle);
            return false;
        }
    }
}
