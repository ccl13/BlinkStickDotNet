using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.Pipes;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BlinkStickDotNet;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBlink.Helper;
using DiscordRPC;
using DiscordRPC.Logging;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiscordBlink
{
    public class DiscordBlinkProgram
    {
        public const string TokenVariableName = "DISCORDBLINKPROGRAM_CURRENT_TOKEN";
        public const string TokenExpirationVariableName = "DISCORDBLINKPROGRAM_CURRENT_TOKEN_EXPIRATION";
        public const string TokenExpirationFormat = "yyyyMMddHHmmssffff";

        public const string ClientId = "749339229422878810";
        public const string ClientKeyEncrypted = @"Zcz9i/i7W0qNYavXvp0G8206c5M1vvdWyFBSLIBO0CJYO1P91NsM2P2nK1q+Exlf";
        public const string RedirectUrl = "https://localhost:62315/";

        public static string ClientKey = null;
        public static string CurrentClientToken
        {
            get
            {
                return System.Environment.GetEnvironmentVariable(TokenVariableName, EnvironmentVariableTarget.User);
            }
            set
            {
                System.Environment.SetEnvironmentVariable(TokenVariableName, value, EnvironmentVariableTarget.User);
            }
        }
        public static DateTime? CurrentTokenTTL
        {
            get
            {
                var value = System.Environment.GetEnvironmentVariable(TokenExpirationVariableName, EnvironmentVariableTarget.User);
                if (string.IsNullOrWhiteSpace(value))
                {
                    return null;
                }
                if (!DateTime.TryParseExact(value, TokenExpirationFormat, null, System.Globalization.DateTimeStyles.AssumeLocal, out var parsed))
                {
                    return null;
                }
                return parsed;
            }
            set
            {
                if (!value.HasValue)
                {
                    return;
                }
                var formatted = value.Value.ToString(TokenExpirationFormat);
                System.Environment.SetEnvironmentVariable(TokenExpirationVariableName, formatted, EnvironmentVariableTarget.User);
            }
        }

        public static string[] DefaultScopes = new string[] {
            "identify",
            "rpc",
            "rpc.notifications.read",
            //"connections",
            //"activities.write",
            //"activities.read",
            //"relationships.read",
            //"rpc.api",
        };

        public static void WaitAfterCancel()
        {
        }

        static void Blink(string[] args)
        {
            Console.WriteLine("Set random color.\r\n");

            BlinkStick[] devices = BlinkStick.FindAll();

            if (devices.Length == 0)
            {
                Console.WriteLine("Could not find any BlinkStick devices...");
                return;
            }

            //Iterate through all of them
            foreach (BlinkStick device in devices)
            {
                //Open the device
                if (device.OpenDevice())
                {
                    Console.WriteLine(string.Format("Device {0} opened successfully", device.Serial));

                    device.SetMode(0);
                    Thread.Sleep(1);

                    int numberOfLeds = 8;

                    for (byte i = 0; i < numberOfLeds; i++)
                    {
                        device.SetColor(0, i, 0, 0, 0);
                        Thread.Sleep(1);
                        Random r = new Random();
                        device.Morph(0, i, (byte)r.Next(32), (byte)r.Next(32), (byte)r.Next(32), 500);
                        Thread.Sleep(1);
                    }
                }
            }

        }

        static async Task<string> GetRPCToken()
        {
            // NOTE: Not validated.

            var postData = new Dictionary<string, string>()
                {
                    { "client_id", DiscordBlinkProgram.ClientId },
                    { "client_secret", DiscordBlinkProgram.ClientKey },
                };

            using (var httpClient = new HttpClient())
            {
                using (var content = new FormUrlEncodedContent(postData))
                {
                    content.Headers.Clear();
                    content.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

                    HttpResponseMessage response = await httpClient.PostAsync("https://discord.com/api/oauth2/token/rpc", content);

                    var responseJson = await response.Content.ReadAsStringAsync();
                    var json = JsonSerializer.Deserialize<Dictionary<string, object>>(responseJson);
                    var access_token = json["access_token"] as string;
                    var ttl = json["expires_in"] as int?;

                    //DiscordBlinkProgram.CurrentClientToken = access_token;
                    //DiscordBlinkProgram.CurrentTokenTTL = ttl.HasValue ? (DateTime?)DateTime.Now.AddSeconds(ttl.Value - 5) : null;
                }
            }

            return null;
        }

        class DiscordRPCCommand
        {
            public string nonce { get; set; }
            public Dictionary<string, object> args { get; set; }
            public string cmd { get; set; }
        }

        static async Task GrabClient()
        {
            while (string.IsNullOrWhiteSpace(CurrentClientToken))
            {
                await Task.Delay(500);
            }

            /*
                Create a Discord client
                NOTE: 	If you are using Unity3D, you must use the full constructor and define
                         the pipe connection.
            */
            var discordRpcClient = new DiscordRpcClient(ClientId);

            //Set the logger
            discordRpcClient.Logger = new ConsoleLogger() { Level = DiscordRPC.Logging.LogLevel.Info };

            //Subscribe to events
            discordRpcClient.OnReady += (sender, e) =>
            {
                Console.WriteLine("Received Ready from user {0}", e.User.Username);
            };

            discordRpcClient.OnPresenceUpdate += (sender, e) =>
            {
                Console.WriteLine("Received Update! {0}", e.Presence);
            };

            //Connect to the RPC
            discordRpcClient.Initialize();


            var nonce = Guid.NewGuid();

            var authorizeCommand = new DiscordRPCCommand()
            {
                nonce = nonce.ToString(),
                args = new Dictionary<string, object>()
                    {
                        { "client_id", ClientId },
                        { "scopes", DefaultScopes },
                    },
                cmd = "AUTHORIZE",
            };

            var authenticateCommand = new DiscordRPCCommand()
            {
                nonce = nonce.ToString(),
                args = new Dictionary<string, object>()
                    {
                        { "access_token", CurrentClientToken },
                    },
                cmd = "AUTHENTICATE",
            };

            //var rpcCLient = new DiscordRpcClient(ClientId, RedirectUrl);

            //var token = await rpcCLient.AuthorizeAsync(defaultScopes);

            //var user = rpcCLient.CurrentUser;

            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", "discord-ipc-0", PipeDirection.InOut))
            {
                // Connect to the pipe or wait until the pipe is available.
                Console.Write("Attempting to connect to pipe...");
                pipeClient.Connect();

                Console.WriteLine("Connected to pipe.");
                Console.WriteLine("There are currently {0} pipe server instances open.",
                   pipeClient.NumberOfServerInstances);

                var buffer = new byte[1048576];

                void ReadCallBack(IAsyncResult result)
                {
                    var text = Encoding.UTF8.GetString(buffer);
                    Console.WriteLine(text);
                }
                var readResult = pipeClient.BeginRead(buffer, 0, buffer.Length, ReadCallBack, buffer);

                using (var writer = new StreamWriter(pipeClient))
                {
                    var authorizeCommandBuffer = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(authorizeCommand));
                    writer.WriteLine(authorizeCommandBuffer);
                    writer.Flush();
                }
                using (var writer = new StreamWriter(pipeClient))
                {
                    var authenticateCommandBuffer = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(authenticateCommand));
                    writer.WriteLine(authenticateCommandBuffer);
                    writer.Flush();
                }

                while (true)
                {
                    var waited = readResult.AsyncWaitHandle.WaitOne(500);
                }
            }



            var port = 6463;
            var version = 1;
            var encoding = "json";

            var websocketClient = new Discord.Net.WebSockets.DefaultWebSocketClient();
            using (var client = new ClientWebSocket())
            {
                client.Options.SetRequestHeader("origin", RedirectUrl);
                websocketClient.SetHeader("origin", RedirectUrl);

                for (port = 6463; port <= 6472; port++)
                {
                    var url = $"ws://127.0.0.1:{port}/?v={version}&client_id={ClientId}&encoding={encoding}";
                    try
                    {
                        await client.ConnectAsync(new Uri(url), CancellationToken.None);
                        await websocketClient.ConnectAsync(url);
                        break;
                    }
                    catch (Exception ex)
                    {

                    }
                }

                websocketClient.BinaryMessage += async (data, index, count) =>
                {
                    using (var compressed = new MemoryStream(data, index + 2, count - 2))
                    using (var decompressed = new MemoryStream())
                    {
                        using (var zlib = new DeflateStream(compressed, CompressionMode.Decompress))
                            zlib.CopyTo(decompressed);
                        decompressed.Position = 0;
                    }
                };
                websocketClient.TextMessage += async text =>
                {
                    Console.WriteLine(text);
                };
                websocketClient.Closed += async ex =>
                {
                };

                var authCommandBuffer = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(authorizeCommand));

                await websocketClient.SendAsync(authCommandBuffer, 0, authCommandBuffer.Length, true);
                await client.SendAsync(authCommandBuffer, WebSocketMessageType.Text, true, CancellationToken.None);

                var authCommandResponseBuffer = WebSocket.CreateClientBuffer(16384, 4096);
                var authResponseResult = await client.ReceiveAsync(authCommandResponseBuffer, CancellationToken.None);
                var authResponse = Encoding.UTF8.GetString(authCommandResponseBuffer);
            }
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("Type in key:");
            var key = Console.ReadLine();
            ClientKey = AESHelper.DecryptStringFromBase64_Aes(ClientKeyEncrypted, key, null);

            var cancelHelper = new CancellableShellHelper();
            cancelHelper.SetupCancelHandler();
            cancelHelper.WaitAfterCancel = WaitAfterCancel;

            var hostBuilder = CreateWebHostBuilder(args);
            IWebHost webHost = hostBuilder.Build();
            var hostTask = webHost.RunAsync();

            var blinkTask = Task.Run(() => Blink(args));

            var rpcTask = GrabClient();

            var runningTasks = new[] {
                hostTask,
                blinkTask,
                rpcTask,
            };

            if (string.IsNullOrWhiteSpace(CurrentClientToken) || DateTime.Now > CurrentTokenTTL)
            {
                var myProcess = new System.Diagnostics.Process();
                myProcess.StartInfo.UseShellExecute = true;
                myProcess.StartInfo.FileName = RedirectUrl;
                myProcess.Start();
            }

            Task.WaitAll(runningTasks, cancelHelper.CancellationToken);
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host
                .CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            return WebHost
                .CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseUrls(RedirectUrl);
        }
    }
}
