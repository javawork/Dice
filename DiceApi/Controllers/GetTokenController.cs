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
    public class GetTokenController : ControllerBase
    {
        [HttpGet]
        public ActionResult<string> Get(string deviceId)
        {
            var token = JwtUtil.GenerateToken(deviceId);
            var response = new GetTokenResponse() {Result = (int)ResultCode.Success, Token = token, GameServerAddress = "127.0.0.1:5101"};
            return JsonConvert.SerializeObject(response);
        }
    }
}