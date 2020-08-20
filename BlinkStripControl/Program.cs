using System;
using BlinkStickDotNet;
using System.Collections.Generic;
using System.Threading;

namespace BlinkStripControl
{
    class Program
    {
        static void Main(string[] args)
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
                    Console.WriteLine(String.Format("Device {0} opened successfully", device.Serial));
                    //Set mode to WS2812. Read more about modes here:
                    //http://www.blinkstick.com/help/tutorials/blinkstick-pro-modes
                    device.SetMode(2);

                    int numberOfLeds = 8;

                    for (byte i = 0; i < numberOfLeds; i++)
                    {
                        Random r = new Random();
                        device.SetColor(0, i, (byte)r.Next(32), (byte)r.Next(32), (byte)r.Next(32));

                        Thread.Sleep(500);
                    }
                }
            }

        }
    }
}
