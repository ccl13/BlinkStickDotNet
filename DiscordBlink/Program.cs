using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlinkStickDotNet;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiscordBlink
{
    public class Program
    {

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

        public static void Main(string[] args)
        {
            var cancelHelper = new CancellableShellHelper();
            cancelHelper.SetupCancelHandler();
            cancelHelper.WaitAfterCancel = WaitAfterCancel;

            var hostBuilder = CreateWebHostBuilder(args);
            IWebHost webHost = hostBuilder.Build();
            var hostTask = webHost.RunAsync();

            var firstBinding = webHost
                .ServerFeatures
                .Get<IServerAddressesFeature>()
                .Addresses
                .First();

            var blinkTask = Task.Run(() => Blink(args));

            var runningTasks = new[] {
                hostTask,
                blinkTask,
            };

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
                .UseStartup<Startup>();
        }
    }
}
