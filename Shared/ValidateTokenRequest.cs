using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    [Serializable]
    public class ValidateTokenRequest
    {
        public string Token { get; set; }
        public string DeviceId { get; set; }
    }
}
