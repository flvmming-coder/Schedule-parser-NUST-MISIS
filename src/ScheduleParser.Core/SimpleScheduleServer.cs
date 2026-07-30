using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ScheduleParser.Core
{
    public sealed class SimpleScheduleServer : IDisposable
    {
        private TcpListener _listener;
        private Thread _thread;
        private volatile bool _running;
        private string _jsonPath;

        public string Prefix { get; private set; }
        public string LocalUrl { get; private set; }
        public string[] NetworkUrls { get; private set; }
        public int Port { get; private set; }

        public bool IsRunning
        {
            get { return _running; }
        }

        public void Start(string addressOrPort, string jsonPath)
        {
            if (_running)
            {
                Stop();
            }

            if (string.IsNullOrWhiteSpace(jsonPath))
            {
                throw new ArgumentException("Не указан файл публикации расписания.");
            }

            int port = ParsePort(addressOrPort);
            _jsonPath = jsonPath;
            Port = port;
            LocalUrl = "http://localhost:" + port.ToString() + "/";
            Prefix = LocalUrl;
            NetworkUrls = BuildNetworkUrls(port);

            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            _running = true;

            _thread = new Thread(ListenLoop);
            _thread.IsBackground = true;
            _thread.Start();
        }

        public void Stop()
        {
            _running = false;
            try
            {
                if (_listener != null)
                {
                    _listener.Stop();
                }
            }
            catch
            {
            }
            finally
            {
                _listener = null;
            }
        }

        public void Dispose()
        {
            Stop();
        }

        private void ListenLoop()
        {
            while (_running)
            {
                try
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(HandleClient, client);
                }
                catch
                {
                    if (!_running)
                    {
                        return;
                    }
                }
            }
        }

        private void HandleClient(object state)
        {
            TcpClient client = state as TcpClient;
            if (client == null)
            {
                return;
            }

            using (client)
            {
                try
                {
                    client.ReceiveTimeout = 5000;
                    client.SendTimeout = 5000;

                    NetworkStream stream = client.GetStream();
                    StreamReader reader = new StreamReader(stream, Encoding.ASCII, false, 1024);
                    string requestLine = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(requestLine))
                    {
                        return;
                    }

                    string line;
                    do
                    {
                        line = reader.ReadLine();
                    }
                    while (!string.IsNullOrEmpty(line));

                    string[] parts = requestLine.Split(' ');
                    string method = parts.Length > 0 ? parts[0].ToUpperInvariant() : "GET";
                    string target = parts.Length > 1 ? parts[1] : "/";

                    if (method == "OPTIONS")
                    {
                        WriteResponse(stream, 204, "text/plain; charset=utf-8", new byte[0]);
                        return;
                    }

                    if (method != "GET" && method != "HEAD")
                    {
                        WriteText(stream, 405, "Метод не поддерживается.");
                        return;
                    }

                    RouteRequest(stream, target, method == "HEAD");
                }
                catch
                {
                }
            }
        }

        private void RouteRequest(NetworkStream stream, string target, bool headOnly)
        {
            string path = ExtractPath(target);

            if (path == "/" || path == "/index.html")
            {
                string html = LoadIndexHtml();
                WriteResponse(stream, 200, "text/html; charset=utf-8", Encoding.UTF8.GetBytes(html), headOnly);
                return;
            }

            if (path == "/schedule.json" || path == "/api/schedule")
            {
                if (!File.Exists(_jsonPath))
                {
                    WriteText(stream, 404, "Расписание еще не опубликовано.", headOnly);
                    return;
                }

                byte[] bytes = File.ReadAllBytes(_jsonPath);
                WriteResponse(stream, 200, "application/json; charset=utf-8", bytes, headOnly);
                return;
            }

            if (path == "/health" || path == "/api/health")
            {
                string status = "{\"status\":\"ok\",\"version\":\"" + AppInfo.Version + "\"}";
                WriteResponse(stream, 200, "application/json; charset=utf-8", Encoding.UTF8.GetBytes(status), headOnly);
                return;
            }

            if (path == "/favicon.ico")
            {
                WriteResponse(stream, 204, "image/x-icon", new byte[0], headOnly);
                return;
            }

            WriteText(stream, 404, "Не найдено.", headOnly);
        }

        private static string ExtractPath(string target)
        {
            if (string.IsNullOrWhiteSpace(target))
            {
                return "/";
            }

            Uri uri;
            if (Uri.TryCreate(target, UriKind.Absolute, out uri))
            {
                target = uri.PathAndQuery;
            }

            int queryIndex = target.IndexOf('?');
            if (queryIndex >= 0)
            {
                target = target.Substring(0, queryIndex);
            }

            if (!target.StartsWith("/", StringComparison.Ordinal))
            {
                target = "/" + target;
            }

            return Uri.UnescapeDataString(target).ToLowerInvariant();
        }

        private static int ParsePort(string addressOrPort)
        {
            if (string.IsNullOrWhiteSpace(addressOrPort))
            {
                return 5088;
            }

            int port;
            if (int.TryParse(addressOrPort.Trim(), out port) && port > 0 && port < 65536)
            {
                return port;
            }

            Uri uri;
            if (Uri.TryCreate(addressOrPort.Trim(), UriKind.Absolute, out uri) && uri.Port > 0)
            {
                return uri.Port;
            }

            throw new ArgumentException("Укажите порт от 1 до 65535, например 5088.");
        }

        private static string[] BuildNetworkUrls(int port)
        {
            List<string> urls = new List<string>();
            try
            {
                IPAddress[] addresses = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
                foreach (IPAddress address in addresses)
                {
                    if (address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address))
                    {
                        urls.Add("http://" + address.ToString() + ":" + port.ToString() + "/");
                    }
                }
            }
            catch
            {
            }

            return urls.ToArray();
        }

        private static string LoadIndexHtml()
        {
            string[] roots = new string[]
            {
                AppDomain.CurrentDomain.BaseDirectory,
                Path.GetDirectoryName(typeof(SimpleScheduleServer).Assembly.Location)
            };

            foreach (string root in roots)
            {
                if (string.IsNullOrWhiteSpace(root))
                {
                    continue;
                }

                string path = Path.Combine(root, "web", "index.html");
                if (File.Exists(path))
                {
                    return File.ReadAllText(path, Encoding.UTF8).Replace("{{VERSION}}", AppInfo.Version);
                }
            }

            return "<!doctype html><html lang=\"ru\"><head><meta charset=\"utf-8\"><title>Расписание</title></head><body><h1>Расписание</h1><p>Файл web/index.html не найден рядом с программой.</p></body></html>";
        }

        private static void WriteText(NetworkStream stream, int statusCode, string text)
        {
            WriteText(stream, statusCode, text, false);
        }

        private static void WriteText(NetworkStream stream, int statusCode, string text, bool headOnly)
        {
            WriteResponse(stream, statusCode, "text/plain; charset=utf-8", Encoding.UTF8.GetBytes(text), headOnly);
        }

        private static void WriteResponse(NetworkStream stream, int statusCode, string contentType, byte[] bytes)
        {
            WriteResponse(stream, statusCode, contentType, bytes, false);
        }

        private static void WriteResponse(NetworkStream stream, int statusCode, string contentType, byte[] bytes, bool headOnly)
        {
            string statusText = GetStatusText(statusCode);
            StringBuilder header = new StringBuilder();
            header.Append("HTTP/1.1 ").Append(statusCode.ToString()).Append(" ").Append(statusText).Append("\r\n");
            header.Append("Content-Type: ").Append(contentType).Append("\r\n");
            header.Append("Content-Length: ").Append(bytes == null ? 0 : bytes.Length).Append("\r\n");
            header.Append("Access-Control-Allow-Origin: *\r\n");
            header.Append("Access-Control-Allow-Methods: GET, OPTIONS\r\n");
            header.Append("Access-Control-Allow-Headers: Content-Type\r\n");
            header.Append("Cache-Control: no-cache\r\n");
            header.Append("Connection: close\r\n");
            header.Append("\r\n");

            byte[] headerBytes = Encoding.ASCII.GetBytes(header.ToString());
            stream.Write(headerBytes, 0, headerBytes.Length);
            if (!headOnly && bytes != null && bytes.Length > 0)
            {
                stream.Write(bytes, 0, bytes.Length);
            }
        }

        private static string GetStatusText(int statusCode)
        {
            switch (statusCode)
            {
                case 200:
                    return "OK";
                case 204:
                    return "No Content";
                case 404:
                    return "Not Found";
                case 405:
                    return "Method Not Allowed";
                default:
                    return "OK";
            }
        }
    }
}
