using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Shared;
using DiceApi.Data;

namespace DiceApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GetTokenController : ControllerBase
    { 
        private readonly ApiConfig _config;
    
        public GetTokenController(ApiConfig config)
        {
            _config = config;
        }

        [HttpGet]
        public ActionResult<string> Get(string deviceId)
        {
            var token = JwtUtil.GenerateToken(deviceId);
            var response = new GetTokenResponse() {Result = (int)ResultCode.Success, Token = token, GameServerAddress = _config.GameServerAddress};
            return JsonConvert.SerializeObject(response);
        }
    }
}