using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using BlinkStickDotNet;
using DiscordBlink.Helper;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiscordBlink
{
    public class DiscordBlinkProgram
    {
        public const string ClientId = "749339229422878810";
        public const string ClientKeyEncrypted = @"Zcz9i/i7W0qNYavXvp0G8206c5M1vvdWyFBSLIBO0CJYO1P91NsM2P2nK1q+Exlf";
        public const string RedirectUrl = "https://localhost:62315/";

        public static string ClientKey = null;
        public static string CurrentClientToken = null;
        public static DateTime? CurrentTokenTTL = null;

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

        static void GrabClient()
        {
            while (string.IsNullOrWhiteSpace(CurrentClientToken))
            {
                Thread.Sleep(500);
            }

            var client = new DiscordRPC.DiscordRpcClient(ClientId);

            client.OnReady += (sender, e) =>
            {
                Console.WriteLine("Received Ready from user {0}", e.User.Username);
            };

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

            var runningTasks = new[] {
                hostTask,
                blinkTask,
            };

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
