using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    [Serializable]
    public class GetTokenResponse
    {
        public int Result;
        public string Token;
        public string GameServerAddress;
    }
}
