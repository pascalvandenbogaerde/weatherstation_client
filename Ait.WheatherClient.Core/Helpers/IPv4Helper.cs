using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Ait.WeatherClient.Core.Helpers
{
    public class IPv4Helper
    {
        public static List<string> GetActiveIP4s()
        {
            // hier wordt een LIST gemaakt met alle IP nummers van 
            // je eigen actieve NICs
            // manueel wordt het loopback adres toegevoegd
            List<string> activeIps = new List<string>();
            activeIps.Add("127.0.0.1");
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    activeIps.Add(ip.ToString());
                }
            }
            return activeIps;
        }
    }
}
