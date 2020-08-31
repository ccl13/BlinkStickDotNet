using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBlink.Helper
{
    public class DeterministicRandomGenerator : System.Security.Cryptography.RandomNumberGenerator
    {
        Random r;

        public DeterministicRandomGenerator(string seed)
        {
            var bytes = Encoding.UTF8.GetBytes(seed);
            var seedNumber = 0;
            for (int i = 0; i < bytes.Length; i += 4)
            {
                var current = new byte[4];
                Array.Copy(bytes, i, current, 0, Math.Min(4, bytes.Length - i));
                var number = BitConverter.ToInt32(current);
                seedNumber ^= number;
            }
            r = new Random(seedNumber);
        }

        public override void GetBytes(byte[] data)
        {
            r.NextBytes(data);
        }

        public override void GetNonZeroBytes(byte[] data)
        {
            // simple implementation
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)r.Next(1, 256);
            }
        }
    }
}
