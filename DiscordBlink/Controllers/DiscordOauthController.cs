using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DiscordBlink.Controllers
{
    [ApiController]
    [Route("")]
    public class DiscordOauthController : ControllerBase
    {
        [HttpGet]
        public RedirectResult Get()
        {
            return new RedirectResult($"https://discord.com/api/oauth2/authorize?client_id={DiscordBlinkProgram.ClientId}&redirect_uri={DiscordBlinkProgram.RedirectUrl}&response_type=code&scope=rpc", false);
        }
    }
}
