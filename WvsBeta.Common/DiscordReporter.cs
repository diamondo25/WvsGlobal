using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WvsBeta.Common
{
    public class DiscordReporter
    {
        public string WebhookURL { get; private set; }
        public static string Username { get; set; }
        public static bool Disabled { get; set; }
        private readonly ConcurrentQueue<string> _messagesToPost = new ConcurrentQueue<string>();
        private Thread _thread = null;

        public const string BanLogURL =
                "discord ban url"
            ;

        public const string ServerTraceURL =
                "discord server trace url"
            ;

        private string ActualUsername 
        {
            get
            {
#if DEBUG
                return Username + "-DEBUG";
#else
                return Username;
#endif
            }
        }

        public DiscordReporter(string webhookUrl)
        {
            WebhookURL = webhookUrl;
            Start();
        }

        public void Enqueue(string message)
        {
            _messagesToPost.Enqueue(message);
        }

        private struct WebhookMessage
        {
            public string content;
            public string username;
        }

        private void Start()
        {
            if (Disabled) return;
            if (_thread != null) return;

            _thread = new Thread(TryToSend);
            _thread.IsBackground = true;
            _thread.Start();
        }

        private void TryToSend()
        {
            while (true)
            {
                while (_messagesToPost.TryDequeue(out string content))
                {
                    var wc = new WebClient();
                    wc.Headers[HttpRequestHeader.ContentType] = "application/json";
                    wc.Proxy = null;
                    try
                    {
                        wc.UploadString(WebhookURL, JsonConvert.SerializeObject(new WebhookMessage
                        {
                            content = content,
                            username = ActualUsername
                        }));
                    }
                    catch
                    {
                        // Some error occurred, try to squash all the messages
                        int msgCount = 1;
                        string totalStr = content;
                        while (totalStr.Length < 400 && msgCount < 10 && _messagesToPost.TryDequeue(out content))
                        {
                            totalStr += "\r\n" + content;
                            msgCount++;
                        }
                        _messagesToPost.Enqueue(totalStr);

                        try
                        {
                            Thread.Sleep(int.Parse(wc.ResponseHeaders["Retry-After"]));
                            continue;
                        }
                        catch { }
                        // Just wait some more
                        Thread.Sleep(5000);
                        break;
                    }
                    Thread.Sleep(200);
                }
                Thread.Sleep(1000);
            }
        }
    }
}
