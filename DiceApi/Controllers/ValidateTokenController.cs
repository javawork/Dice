using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Shared;

namespace DiceApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValidateTokenController : ControllerBase
    {
        [HttpPost]
        public ActionResult<string> Post([FromBody]ValidateTokenRequest request)
        {
            var tokenResult = JwtUtil.IsValidToken(request.Token, request.DeviceId);
            var response = new ValidateTokenResponse() { Result = (tokenResult == JwtUtil.ValidateTokenResult.Success) ? (int)ResultCode.Success : (int)ResultCode.InvalidToken  };
            return JsonConvert.SerializeObject(response);
        }
    }
}