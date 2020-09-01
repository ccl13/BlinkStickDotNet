using System;
using System.Threading;
using System.Threading.Tasks;
using BlinkStickDotNet;
using DiscordBlink.Helper;
using DiscordRPC;
using DiscordRPC.Logging;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace DiscordBlink
{
    public class DiscordBlinkProgram
    {
        public const string TokenVariableName = "DISCORDBLINKPROGRAM_CURRENT_TOKEN";
        public const string TokenExpirationVariableName = "DISCORDBLINKPROGRAM_CURRENT_TOKEN_EXPIRATION";
        public const string TokenExpirationFormat = "yyyyMMddHHmmssffff";

        public const string ClientId = "749339229422878810";
        public const string ClientKeyEncrypted = @"Zcz9i/i7W0qNYavXvp0G8206c5M1vvdWyFBSLIBO0CJYO1P91NsM2P2nK1q+Exlf";
        public const string RedirectUrl = "http://localhost:62315/";

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

        public static CancellableShellHelper CancellableShellHelper = new CancellableShellHelper();

        public static BlinkStick[] BlinkDevices = null;
        public const byte NumberOfBlinkLights = 8;

        public static bool? LastIsMuted = null;
        public static bool? DoneSetLastIsMuted = null;

        static async Task BlinkSetup()
        {
            Console.WriteLine("Set random color.\r\n");

            var blinkDevices = BlinkStick.FindAll();

            if (blinkDevices.Length == 0)
            {
                Console.WriteLine("Could not find any BlinkStick devices...");
                return;
            }

            // Iterate through all of them
            foreach (BlinkStick device in blinkDevices)
            {
                //Open the device
                if (device.OpenDevice())
                {
                    Console.WriteLine(string.Format("Device {0} opened successfully", device.Serial));

                    device.SetMode(0);
                    await Task.Delay(1);

                    for (byte i = 1; i <= NumberOfBlinkLights; i++)
                    {
                        device.SetColor(0, i, 0, 0, 0);
                        await Task.Delay(1);
                        //Random r = new Random();
                        //device.Morph(0, i, (byte)r.Next(32), (byte)r.Next(32), (byte)r.Next(32), 500);
                        //await Task.Delay(1);
                    }
                }
            }

            BlinkDevices = blinkDevices;

            while (!CancellableShellHelper.CancellationToken.WaitHandle.WaitOne(200))
            {
                if (!LastIsMuted.HasValue)
                {
                    continue;
                }
                // Iterate through all of them
                foreach (BlinkStick device in BlinkDevices)
                {
                    //Open the device
                    if (device.OpenDevice())
                    {
                        if (LastIsMuted.Value && (!DoneSetLastIsMuted.HasValue || !DoneSetLastIsMuted.Value))
                        {
                            device.Morph(0, 1, 32, 0, 0, 100, 10);
                            DoneSetLastIsMuted = true;
                        }
                        else if (!LastIsMuted.Value && (!DoneSetLastIsMuted.HasValue || DoneSetLastIsMuted.Value))
                        {
                            device.Morph(0, 1, 0, 0, 32, 100, 10);
                            DoneSetLastIsMuted = false;
                        }
                        Task.Delay(1, CancellableShellHelper.CancellationToken).Wait();
                        device.CloseDevice();
                    }
                }
            }

            foreach (BlinkStick device in blinkDevices)
            {
                device.CloseDevice();
            }
        }

        static void ProcessDiscordVoiceStatus(object sender, DiscordRPC.Message.VoiceSettingsMessage args)
        {
            LastIsMuted = args.IsMuted;
        }

        static async Task DiscordSetup()
        {
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

            discordRpcClient.Authorize(DefaultScopes);

            while (string.IsNullOrWhiteSpace(discordRpcClient.AccessCode))
            {
                await Task.Delay(500, CancellableShellHelper.CancellationToken);
            }

            discordRpcClient.Authenticate(null);

            while (string.IsNullOrWhiteSpace(discordRpcClient.AccessToken))
            {
                await Task.Delay(500, CancellableShellHelper.CancellationToken);
            }

            discordRpcClient.RegisterUriScheme();

            discordRpcClient.GetVoiceSettings();

            discordRpcClient.Subscribe(EventType.VoiceSettingsUpdate);

            discordRpcClient.OnVoiceSettingsUpdate += ProcessDiscordVoiceStatus;

            while (!CancellableShellHelper.CancellationToken.WaitHandle.WaitOne(0))
            {
                await Task.Delay(500, CancellableShellHelper.CancellationToken);
            }

            discordRpcClient.ClearPresence();

            discordRpcClient.ShutdownOnly = true;
        }

        public static async Task KickOffHosting(string[] args)
        {
            try
            {
                var hostBuilder = CreateWebHostBuilder(args);
                IWebHost webHost = hostBuilder.Build();
                var task = webHost.RunAsync(CancellableShellHelper.CancellationToken);

                Console.WriteLine("Type in key:");
                var key = Console.ReadLine();
                ClientKey = AESHelper.DecryptStringFromBase64_Aes(ClientKeyEncrypted, key, null);

                if (string.IsNullOrWhiteSpace(CurrentClientToken) || DateTime.Now > CurrentTokenTTL)
                {
                    var myProcess = new System.Diagnostics.Process();
                    myProcess.StartInfo.UseShellExecute = true;
                    myProcess.StartInfo.FileName = RedirectUrl;
                    myProcess.Start();
                }
                await task;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static void GracefulShutdown()
        {
            if (BlinkDevices == null || BlinkDevices.Length <= 0)
            {
                return;
            }

            // Iterate through all of them
            foreach (BlinkStick device in BlinkDevices)
            {
                //Open the device
                if (device.OpenDevice())
                {
                    for (byte i = 1; i <= NumberOfBlinkLights; i++)
                    {
                        device.SetColor(0, i, 0, 0, 0);
                        Thread.Sleep(1);
                    }
                }
            }

        }

        public static void Main(string[] args)
        {
            CancellableShellHelper.SetupCancelHandler();
            CancellableShellHelper.WaitAfterCancel = GracefulShutdown;

            var hostTask = KickOffHosting(args);
            var blinkTask = BlinkSetup();
            var rpcTask = DiscordSetup();

            var runningTasks = new[] {
                hostTask,
                blinkTask,
                rpcTask,
            };

            Task.WaitAll(runningTasks, CancellableShellHelper.CancellationToken);
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
