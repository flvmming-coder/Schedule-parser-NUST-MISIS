using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;

namespace ScheduleParser.Core
{
    public static class NetworkHelper
    {
        public static bool IsNetworkAvailable()
        {
            return NetworkInterface.GetIsNetworkAvailable();
        }

        public static bool IsInternetAvailable()
        {
            if (!IsNetworkAvailable())
            {
                return false;
            }

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.github.com/rate_limit");
                request.Method = "GET";
                request.UserAgent = "ScheduleParser";
                request.Timeout = 4000;

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    int code = (int)response.StatusCode;
                    return code >= 200 && code < 500;
                }
            }
            catch
            {
                return false;
            }
        }

        public static string DownloadStringWithTimeout(string url, int timeoutMilliseconds)
        {
            using (WebClient client = new WebClient())
            {
                client.Encoding = System.Text.Encoding.UTF8;
                ManualResetEvent done = new ManualResetEvent(false);
                string result = null;
                Exception error = null;

                client.DownloadStringCompleted += delegate(object sender, DownloadStringCompletedEventArgs e)
                {
                    error = e.Error;
                    if (!e.Cancelled && e.Error == null)
                    {
                        result = e.Result;
                    }

                    done.Set();
                };

                client.DownloadStringAsync(new Uri(url));
                if (!done.WaitOne(timeoutMilliseconds))
                {
                    client.CancelAsync();
                    throw new TimeoutException("Не удалось получить расписание: превышено время ожидания.");
                }

                if (error != null)
                {
                    throw error;
                }

                return result;
            }
        }
    }
}
