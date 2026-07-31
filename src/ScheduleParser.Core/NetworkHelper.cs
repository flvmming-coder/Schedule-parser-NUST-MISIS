using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;

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

        public static string NormalizeScheduleJsonUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentException("Не указан URL сервера расписания.");
            }

            url = url.Trim().Trim('"');
            if (IsExistingLocalFile(url))
            {
                return new Uri(Path.GetFullPath(url)).AbsoluteUri;
            }

            if (LooksLikeHostAddressWithoutScheme(url))
            {
                url = "http://" + url;
            }

            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                throw new ArgumentException("Некорректный URL сервера расписания.");
            }

            if (string.Equals(uri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
            {
                return uri.AbsoluteUri;
            }

            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Поддерживаются HTTP, HTTPS, file:// и обычный путь к JSON-файлу.");
            }

            UriBuilder uriBuilder = new UriBuilder(uri);
            if (string.Equals(uriBuilder.Host, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                uriBuilder.Host = "127.0.0.1";
            }

            string path = uri.AbsolutePath;
            if (string.IsNullOrWhiteSpace(path) || path == "/")
            {
                uriBuilder.Path = "schedule.json";
                uriBuilder.Query = string.Empty;
            }

            return uriBuilder.Uri.ToString();
        }

        public static string LoadScheduleText(string source, int timeoutMilliseconds)
        {
            string normalized = NormalizeScheduleJsonUrl(source);
            Uri uri = new Uri(normalized);
            if (string.Equals(uri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
            {
                string path = uri.LocalPath;
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException("JSON-файл расписания не найден.", path);
                }

                return File.ReadAllText(path, Encoding.UTF8);
            }

            return DownloadStringWithTimeout(normalized, timeoutMilliseconds);
        }

        public static string DownloadStringWithTimeout(string url, int timeoutMilliseconds)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Accept = "application/json,text/plain,*/*";
            request.UserAgent = AppInfo.ProductName + "/" + AppInfo.Version;
            request.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
            request.KeepAlive = false;
            request.Timeout = timeoutMilliseconds;
            request.ReadWriteTimeout = timeoutMilliseconds;
            request.Proxy = null;

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, true))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.Timeout)
                {
                    throw new TimeoutException("Не удалось получить расписание: превышено время ожидания ответа сервера.", ex);
                }

                throw new InvalidOperationException(BuildConnectionErrorMessage(url, ex), ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Не удалось загрузить расписание: " + ex.Message, ex);
            }
        }

        private static string BuildConnectionErrorMessage(string url, WebException error)
        {
            HttpWebResponse response = error.Response as HttpWebResponse;
            if (response != null)
            {
                return string.Format("Сервер расписания ответил ошибкой {0}. Проверьте адрес: {1}", (int)response.StatusCode, url);
            }

            return "Не удалось подключиться к серверу расписания. Проверьте, что сервер запущен, устройство находится в той же сети, а Windows Firewall разрешил доступ. URL: " + url;
        }

        private static bool IsExistingLocalFile(string value)
        {
            try
            {
                return File.Exists(value);
            }
            catch
            {
                return false;
            }
        }

        private static bool LooksLikeHostAddressWithoutScheme(string value)
        {
            string lower = value.ToLowerInvariant();
            return lower.StartsWith("localhost", StringComparison.Ordinal) ||
                   lower.StartsWith("127.", StringComparison.Ordinal) ||
                   lower.StartsWith("192.168.", StringComparison.Ordinal) ||
                   lower.StartsWith("10.", StringComparison.Ordinal) ||
                   lower.StartsWith("172.", StringComparison.Ordinal) ||
                   lower.StartsWith("[::1]", StringComparison.Ordinal);
        }
    }
}
