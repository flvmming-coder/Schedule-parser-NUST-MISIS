using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace ScheduleParser.Core
{
    public sealed class GitHubPagesPublisher
    {
        private readonly JavaScriptSerializer _serializer;

        public GitHubPagesPublisher(string owner, string repository, string branch)
        {
            Owner = owner;
            Repository = repository;
            Branch = string.IsNullOrWhiteSpace(branch) ? "gh-pages" : branch;
            _serializer = new JavaScriptSerializer();
            _serializer.MaxJsonLength = int.MaxValue;
        }

        public string Owner { get; private set; }
        public string Repository { get; private set; }
        public string Branch { get; private set; }

        public GitHubPublishResult Publish(ScheduleDocument document, string webIndexPath, string token)
        {
            if (document == null)
            {
                throw new ArgumentNullException("document");
            }

            try
            {
                string resolvedToken = ResolveToken(token);
                EnsureBranch(resolvedToken);

                string indexHtml = LoadIndexHtml(webIndexPath);
                string scheduleJson = ScheduleJsonSerializer.ToJson(document);

                PutTextFile("index.html", indexHtml, "Publish global schedule web viewer", resolvedToken);
                PutTextFile("schedule.json", scheduleJson, "Publish global schedule data", resolvedToken);

                GitHubPublishResult result = new GitHubPublishResult();
                result.PageUrl = "https://" + Owner + ".github.io/" + Repository + "/";
                result.ScheduleJsonUrl = result.PageUrl + "schedule.json";
                return result;
            }
            catch (WebException ex)
            {
                throw new InvalidOperationException("GitHub API вернул ошибку: " + ReadError(ex), ex);
            }
        }

        private string LoadIndexHtml(string webIndexPath)
        {
            if (!string.IsNullOrWhiteSpace(webIndexPath) && File.Exists(webIndexPath))
            {
                return File.ReadAllText(webIndexPath, Encoding.UTF8).Replace("{{VERSION}}", AppInfo.Version);
            }

            string fallback = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "web", "index.html");
            if (File.Exists(fallback))
            {
                return File.ReadAllText(fallback, Encoding.UTF8).Replace("{{VERSION}}", AppInfo.Version);
            }

            throw new FileNotFoundException("Не найден web/index.html для публикации в интернет.");
        }

        private void EnsureBranch(string token)
        {
            if (RefExists(Branch, token))
            {
                return;
            }

            Dictionary<string, object> masterRef = ReadJsonObject("GET", ApiUrl("git/ref/heads/master"), token, null);
            Dictionary<string, object> masterObject = masterRef["object"] as Dictionary<string, object>;
            string sha = masterObject == null ? null : Convert.ToString(masterObject["sha"]);
            if (string.IsNullOrWhiteSpace(sha))
            {
                throw new InvalidOperationException("Не удалось получить SHA ветки master для создания gh-pages.");
            }

            Dictionary<string, object> body = new Dictionary<string, object>();
            body["ref"] = "refs/heads/" + Branch;
            body["sha"] = sha;
            ApiRequest("POST", ApiUrl("git/refs"), token, _serializer.Serialize(body), "application/json");
        }

        private bool RefExists(string branch, string token)
        {
            try
            {
                ApiRequest("GET", ApiUrl("git/ref/heads/" + branch), token, null, null);
                return true;
            }
            catch (WebException ex)
            {
                HttpWebResponse response = ex.Response as HttpWebResponse;
                if (response != null && response.StatusCode == HttpStatusCode.NotFound)
                {
                    return false;
                }

                throw;
            }
        }

        private void PutTextFile(string path, string text, string message, string token)
        {
            string sha = GetExistingFileSha(path, token);
            Dictionary<string, object> body = new Dictionary<string, object>();
            body["message"] = message;
            body["branch"] = Branch;
            body["content"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
            if (!string.IsNullOrWhiteSpace(sha))
            {
                body["sha"] = sha;
            }

            ApiRequest("PUT", ApiUrl("contents/" + Uri.EscapeDataString(path)), token, _serializer.Serialize(body), "application/json");
        }

        private string GetExistingFileSha(string path, string token)
        {
            try
            {
                string url = ApiUrl("contents/" + Uri.EscapeDataString(path)) + "?ref=" + Uri.EscapeDataString(Branch);
                Dictionary<string, object> response = ReadJsonObject("GET", url, token, null);
                return response.ContainsKey("sha") ? Convert.ToString(response["sha"]) : null;
            }
            catch (WebException ex)
            {
                HttpWebResponse response = ex.Response as HttpWebResponse;
                if (response != null && response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                throw;
            }
        }

        private Dictionary<string, object> ReadJsonObject(string method, string url, string token, string body)
        {
            string json = ApiRequest(method, url, token, body, body == null ? null : "application/json");
            return _serializer.Deserialize<Dictionary<string, object>>(json);
        }

        private string ApiRequest(string method, string url, string token, string body, string contentType)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = method;
            request.Accept = "application/vnd.github+json";
            request.UserAgent = AppInfo.ProductName + "/" + AppInfo.Version;
            request.Headers[HttpRequestHeader.Authorization] = "Bearer " + token;
            request.Headers["X-GitHub-Api-Version"] = "2022-11-28";
            request.Timeout = 20000;
            request.ReadWriteTimeout = 20000;

            if (body != null)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(body);
                request.ContentType = contentType ?? "application/json";
                request.ContentLength = bytes.Length;
                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(bytes, 0, bytes.Length);
                }
            }

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, true))
            {
                return reader.ReadToEnd();
            }
        }

        private string ApiUrl(string path)
        {
            return "https://api.github.com/repos/" + Owner + "/" + Repository + "/" + path.TrimStart('/');
        }

        private static string ReadError(WebException ex)
        {
            HttpWebResponse response = ex.Response as HttpWebResponse;
            if (response == null)
            {
                return ex.Message;
            }

            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, true))
            {
                string text = reader.ReadToEnd();
                return ((int)response.StatusCode).ToString() + " " + response.StatusCode + " " + text;
            }
        }

        private static string ResolveToken(string token)
        {
            if (!string.IsNullOrWhiteSpace(token))
            {
                return token.Trim();
            }

            string env = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            if (!string.IsNullOrWhiteSpace(env))
            {
                return env.Trim();
            }

            string credential = TryReadGitCredentialToken();
            if (!string.IsNullOrWhiteSpace(credential))
            {
                return credential;
            }

            throw new InvalidOperationException("Не найден GitHub token. Введите token в поле публикации или выполните вход в GitHub через Git Credential Manager.");
        }

        private static string TryReadGitCredentialToken()
        {
            try
            {
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = "git";
                info.Arguments = "credential fill";
                info.UseShellExecute = false;
                info.RedirectStandardInput = true;
                info.RedirectStandardOutput = true;
                info.RedirectStandardError = true;
                info.CreateNoWindow = true;

                using (Process process = Process.Start(info))
                {
                    process.StandardInput.Write("protocol=https\nhost=github.com\n\n");
                    process.StandardInput.Close();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(5000);
                    string[] lines = output.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("password=", StringComparison.OrdinalIgnoreCase))
                        {
                            return line.Substring("password=".Length).Trim();
                        }
                    }
                }
            }
            catch
            {
            }

            return null;
        }
    }

    public sealed class GitHubPublishResult
    {
        public string PageUrl { get; set; }
        public string ScheduleJsonUrl { get; set; }
    }
}
