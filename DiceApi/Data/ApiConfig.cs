using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiceApi.Data
{
    public class ApiConfig
    {
        public string GameServerAddress;
        public ApiConfig(string gameServerAddress)
        {
            GameServerAddress = gameServerAddress;
        }
    }
}
