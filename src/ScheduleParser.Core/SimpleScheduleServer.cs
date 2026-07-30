using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace ScheduleParser.Core
{
    public sealed class SimpleScheduleServer : IDisposable
    {
        private readonly HttpListener _listener;
        private Thread _thread;
        private volatile bool _running;
        private string _jsonPath;

        public SimpleScheduleServer()
        {
            _listener = new HttpListener();
        }

        public string Prefix { get; private set; }

        public bool IsRunning
        {
            get { return _running; }
        }

        public void Start(string prefix, string jsonPath)
        {
            if (_running)
            {
                Stop();
            }

            if (string.IsNullOrWhiteSpace(prefix))
            {
                throw new ArgumentException("Не указан HTTP-адрес сервера.");
            }

            if (!prefix.EndsWith("/", StringComparison.Ordinal))
            {
                prefix += "/";
            }

            _jsonPath = jsonPath;
            Prefix = prefix;
            _listener.Prefixes.Clear();
            _listener.Prefixes.Add(prefix);
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
                if (_listener.IsListening)
                {
                    _listener.Stop();
                }
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            Stop();
            _listener.Close();
        }

        private void ListenLoop()
        {
            while (_running)
            {
                try
                {
                    HttpListenerContext context = _listener.GetContext();
                    Handle(context);
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

        private void Handle(HttpListenerContext context)
        {
            string path = context.Request.Url.AbsolutePath.Trim('/').ToLowerInvariant();
            if (path.Length == 0 || path == "schedule.json")
            {
                if (!File.Exists(_jsonPath))
                {
                    Write(context, 404, "text/plain; charset=utf-8", "Расписание еще не опубликовано.");
                    return;
                }

                byte[] bytes = File.ReadAllBytes(_jsonPath);
                context.Response.ContentType = "application/json; charset=utf-8";
                context.Response.StatusCode = 200;
                context.Response.ContentLength64 = bytes.Length;
                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                context.Response.OutputStream.Close();
                return;
            }

            Write(context, 404, "text/plain; charset=utf-8", "Не найдено.");
        }

        private static void Write(HttpListenerContext context, int statusCode, string contentType, string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = contentType;
            context.Response.ContentLength64 = bytes.Length;
            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
            context.Response.OutputStream.Close();
        }
    }
}
