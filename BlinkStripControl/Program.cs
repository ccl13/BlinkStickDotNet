using System;
using BlinkStickDotNet;
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
    }
}
