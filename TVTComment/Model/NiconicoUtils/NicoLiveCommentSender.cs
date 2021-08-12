using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Websocket.Client;

namespace TVTComment.Model.NiconicoUtils
{

    [Serializable]
    class NicoLiveCommentSenderException : Exception
    {
        public NicoLiveCommentSenderException() { }
        public NicoLiveCommentSenderException(string message) : base(message) { }
        public NicoLiveCommentSenderException(string message, Exception inner) : base(message, inner) { }
        protected NicoLiveCommentSenderException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    class InvalidPlayerStatusNicoLiveCommentSenderException : NicoLiveCommentSenderException
    {
        public string PlayerStatus { get; }
        public InvalidPlayerStatusNicoLiveCommentSenderException(string playerStatus)
        {
            PlayerStatus = playerStatus;
        }
    }

    class NetworkNicoLiveCommentSenderException : NicoLiveCommentSenderException
    {
        public NetworkNicoLiveCommentSenderException(Exception inner) : base(null, inner)
        {
        }
    }
    class ResponseFormatNicoLiveCommentSenderException : NicoLiveCommentSenderException
    {
        public string Response { get; }

        public ResponseFormatNicoLiveCommentSenderException(string response)
        {
            Response = response;
        }
    }

    class ResponseErrorNicoLiveCommentSenderException : NicoLiveCommentSenderException
    {
    }

    class NicoLiveCommentSender : IDisposable
    {
        private readonly HttpClient httpClient;
        private BlockingCollection<string[]> messageColl = new ();

        public NicoLiveCommentSender(NiconicoLoginSession niconicoSession)
        {
            var handler = new HttpClientHandler();
            handler.CookieContainer.Add(niconicoSession.Cookie);
            httpClient = new HttpClient(handler);
            var assembly = Assembly.GetExecutingAssembly().GetName();
            var ua = assembly.Name + "/" + assembly.Version.ToString(3);
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", ua);
        }


        public async Task ConnectWatchSession(string liveId, CancellationToken cancellationToken)
        {
            var resp = await httpClient.GetStringAsync("https://live.nicovideo.jp/watch/" + liveId).ConfigureAwait(false);
            var webSocketUrl = Regex.Matches(resp, @"wss://.+nicovideo.jp/[/a-z0-9]+[0-9]+\?audience_token=([_a-z0-9]*)").First().Value;
            int.TryParse(Regex.Matches(resp, @"&quot;beginTime&quot;:(?<time>[0-9]+),").First().Groups["time"].Value,out var openTime);

            var _ = Task.Run( async () =>
            {
                var factory = new Func<ClientWebSocket>(() => {

                    var cl = new ClientWebSocket();
                    var assembly = Assembly.GetExecutingAssembly().GetName();
                    var ua = assembly.Name + "/" + assembly.Version.ToString(3);
                    cl.Options.SetRequestHeader("User-Agent", ua);
                    return cl;
                });
                using var ws = new WebsocketClient(new Uri(webSocketUrl), factory);

                using var timer = new System.Timers.Timer(30 * 1000);

                ElapsedEventHandler handler = null;
                handler = async (sender, e) =>
                {
                    if (!ws.IsRunning)
                    {
                        timer.Elapsed -= handler;
                        timer.Close();
                        return;
                    }
                    await ws.SendInstant("{\"type\": \"keepSeat\"}");
                };

                ws.MessageReceived
                    .Subscribe(async msg =>
                    {
                        var json = JsonDocument.Parse(msg.Text).RootElement;
                        var type = json.GetProperty("type").GetString();
                        switch (type)
                        {
                            case "error":
                                break;
                            case "seat":
                                var keepInterval = json.GetProperty("data").GetProperty("keepIntervalSec").GetInt32();
                                timer.Interval = keepInterval * 1000;
                                timer.Elapsed += handler;
                                timer.Start();
                                break;
                            case "ping":
                                await ws.SendInstant("{\"type\": \"pong\"}");
                                break;
                            case "reconnect":
                                var data = json.GetProperty("data");
                                var token = data.GetProperty("audienceToken").GetString();
                                var waittime = data.GetProperty("waitTimeSec").GetInt32();
                                await ws.Stop(WebSocketCloseStatus.NormalClosure, "reconnect");
                                /*ws.Stop(WebSocketCloseStatus.NormalClosure, "reconnect");
                                ws.Url = new Uri(Regex.Replace(webSocketUrl, @"", ""));
                                ws.Start();*/
                                break;
                            case "disconnect":
                                await ws.Stop(WebSocketCloseStatus.NormalClosure, "disconnect");
                                break;
                            case "postCommentResult":
                                break;
                        }
                    });

                await ws.Start();
                await ws.SendInstant("{\"type\": \"startWatching\",\"data\": {\"reconnect\": false}}");

                while(true)
                {
                    if (!ws.IsStarted || cancellationToken.IsCancellationRequested)
                    {
                        await ws.Stop(WebSocketCloseStatus.NormalClosure, "IsCancellationRequested");
                        timer.Elapsed -= handler;
                        timer.Close();
                        break;
                    }
                    while (messageColl.TryTake(out var text))
                    {
                        long vpos = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 10 - openTime * 100; // vposは10ミリ秒単位
                        await ws.SendInstant("{ \"type\": \"postComment\", \"data\": { \"text\": \"" + text[0] + "\", \"vpos\":" + vpos + ", \"isAnonymous\": true } }");
                    }
                }

            }, cancellationToken);
        }

        /// <summary>
        /// コメントを投稿する
        /// </summary>
        public async Task Send(string liveId, string message, string mail)
        {
            messageColl.Add(new string[] { message, mail});
        }

        public void Dispose()
        {
            httpClient.Dispose();

        }
    }
}
