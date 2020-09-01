using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DiscordBlink.Controllers
{
    [ApiController]
    [Route("")]
    public class DiscordOauthController : ControllerBase
    {

        public RedirectResult AuthRedirect()
        {
            return new RedirectResult($"https://discord.com/api/oauth2/authorize?client_id={DiscordBlinkProgram.ClientId}&redirect_uri={DiscordBlinkProgram.RedirectUrl}&response_type=code&scope={string.Join(" ", DiscordBlinkProgram.DefaultScopes)}", false);
        }

        [HttpGet]
        public async Task<ActionResult> Get(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return AuthRedirect();
            }

            /*Get Access Token from authorization code by making http post request*/
            var postData = new Dictionary<string, string>()
                {
                    { "client_id", DiscordBlinkProgram.ClientId },
                    { "client_secret", DiscordBlinkProgram.ClientKey },
                    { "grant_type", "client_credentials" },
                    { "code", code },
                    { "redirect_uri", DiscordBlinkProgram.RedirectUrl },
                    { "scope", string.Join(" ", DiscordBlinkProgram.DefaultScopes) },
                };

            using (var httpClient = new HttpClient())
            {
                using (var content = new FormUrlEncodedContent(postData))
                {
                    content.Headers.Clear();
                    content.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

                    HttpResponseMessage response = await httpClient.PostAsync("https://discord.com/api/oauth2/token", content);

                    var responseJson = await response.Content.ReadAsStringAsync();
                    var json = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseJson);
                    var access_token = json["access_token"].GetString();
                    var ttl = json["expires_in"].GetInt32();

                    DiscordBlinkProgram.CurrentClientToken = access_token;
                    DiscordBlinkProgram.CurrentTokenTTL = (DateTime?)DateTime.Now.AddSeconds(ttl - 5);
                }
            }

            return new OkResult();
        }

    }
}
