using System;
using BlinkStickDotNet;
using System.Threading;

namespace BlinkStripControl
{
    class BlinkTestMain
    {

        public static void WaitAfterCancel()
        {
        }


        public const byte Channel = 0;
        public const byte NumberOfLights = 8;

        static void Main(string[] args)
        {
            var cancelHelper = new CancellableShellHelper();
            cancelHelper.SetupCancelHandler();
            cancelHelper.WaitAfterCancel = WaitAfterCancel;

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

                    for (byte i = 0; i < NumberOfLights; i++)
                    {
                        device.SetColor(0, i, 0, 0, 0);
                        Thread.Sleep(100);
                        Random r = new Random();
                        device.Morph(Channel, i, (byte)r.Next(32), (byte)r.Next(32), (byte)r.Next(32), 500, 30);
                        Thread.Sleep(1);
                    }
                }
            }

        }
    }
}
