using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NetworkLimiter
{
    public class NetworkActivity
    {
        public string Hostname { get; set; }
        public string IP { get; set; }
        public double Download { get; set; }
        public double Upload { get; set; }
        public double DownloadLimit { get; set; }
        public double UploadLimit { get; set; }
        public string TotalSend { get; set; }
        public string TotalRecv { get; set; }

        private static List<NetworkActivity> parseText(string m)
        {
            List<NetworkActivity> networkList = new List<NetworkActivity>();
            using (StringReader reader = new StringReader(m))
            {
                // Skip First Line for Headings
                reader.ReadLine();
                string line = string.Empty;
                do
                {
                    line = reader.ReadLine();
                    if (line != null)
                    {
                        string[] t = line.Split(',');
                        // added /s for download and Upload for per second
                        networkList.Add(new NetworkActivity (t[0] , double.Parse(t[1]) , double.Parse(t[2]), Conversion(double.Parse(t[3])), Conversion(double.Parse(t[4]))));
                    }
                } while (line != null);
            }
            //List<NetworkActivity> SortedList = networkList.OrderBy(o => o.IP).ToList();
            return networkList;
        }

        private static string Conversion(double value)
        {
            // if more than 1 million used Megabytes
            if (value > 1000000)
                return ConvertToMbps(value).ToString() + " MB";
            else
                return ConvertToKbps(value).ToString() + " kB";
        }

        private static double ConvertToKbps(double value)
        {
            // 1024 bytes in kilobyte Round 2 Decimals
            return Math.Round(value / 1024,2);
        }

        private static double ConvertToMbps(double value)
        {
            // 1024 * 1024 bytes in megabytes Round 2 Decimals
            return Math.Round(value / (1024 * 1024), 2);
        }

        private static string GetHtmlText(string Link)
        {
            HtmlAgilityPack.HtmlWeb web = new HtmlAgilityPack.HtmlWeb();
            HtmlAgilityPack.HtmlDocument doc = web.Load(Link);
            return doc.DocumentNode.InnerText;
        }

        private NetworkActivity(string IP, double Upload, double Download, string TotalSend, string TotalRecv)
        {
            this.IP = IP;
            this.Download = Download;
            this.Upload = Upload;
            this.TotalSend = TotalSend;
            this.TotalRecv = TotalRecv;
        }

        public static List<NetworkActivity> getNeworkActivity()
        {
            string rawHtml = (GetHtmlText("Http://localhost"));
            return parseText(rawHtml);
        }
    }
}
