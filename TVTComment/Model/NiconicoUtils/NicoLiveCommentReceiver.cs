using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TVTComment.Model.NiconicoUtils
{
    class NicoLiveCommentReceiverException : Exception
    {
        public NicoLiveCommentReceiverException() { }
        public NicoLiveCommentReceiverException(string message) : base(message) { }
        public NicoLiveCommentReceiverException(string message, Exception inner) : base(message, inner) { }
    }

    class InvalidPlayerStatusNicoLiveCommentReceiverException : NicoLiveCommentReceiverException
    {
        public string PlayerStatus { get; }
        public InvalidPlayerStatusNicoLiveCommentReceiverException(string playerStatus)
        {
            PlayerStatus = playerStatus;
        }
    }

    class NetworkNicoLiveCommentReceiverException : NicoLiveCommentReceiverException
    {
        public NetworkNicoLiveCommentReceiverException(Exception inner) : base(null, inner)
        {
        }
    }
    class ConnectionClosedNicoLiveCommentReceiverException : NicoLiveCommentReceiverException
    {
        public ConnectionClosedNicoLiveCommentReceiverException()
        {
        }
    }

    class NicoLiveCommentReceiver : IDisposable
    {
        public NiconicoLoginSession NiconicoLoginSession { get; }
        private int count;
        private readonly string ua;

        public NicoLiveCommentReceiver(NiconicoLoginSession niconicoLoginSession)
        {
            NiconicoLoginSession = niconicoLoginSession;

            var handler = new HttpClientHandler();
            handler.CookieContainer.Add(niconicoLoginSession.Cookie);
            httpClient = new HttpClient(handler);
            var assembly = Assembly.GetExecutingAssembly().GetName();
            ua = assembly.Name + "/" + assembly.Version.ToString(3);
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", ua);
            count = Environment.TickCount;
        }

        /// <summary>
        /// 受信した<see cref="NiconicoCommentXmlTag"/>を無限非同期イテレータで返す
        /// </summary>
        /// <exception cref="OperationCanceledException"></exception>
        /// <exception cref="InvalidPlayerStatusNicoLiveCommentReceiverException"></exception>
        /// <exception cref="NetworkNicoLiveCommentReceiverException"></exception>
        /// <exception cref="ConnectionClosedNicoLiveCommentReceiverException"></exception>
        public async IAsyncEnumerable<NiconicoCommentXmlTag> Receive(string liveId, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var _ = cancellationToken.Register(() =>
           {
               httpClient.CancelPendingRequests();
           });

            for (int disconnectedCount = 0; disconnectedCount < 5; ++disconnectedCount)
            {
                Stream str;
                try
                {
                    str = await httpClient.GetStreamAsync($"https://live2.nicovideo.jp/unama/watch/{liveId}/programinfo", cancellationToken).ConfigureAwait(false);
                }
                // httpClient.CancelPendingRequestsが呼ばれた、もしくはタイムアウト
                catch (TaskCanceledException e)
                {
                    if (cancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(null, e, cancellationToken);
                    throw new NetworkNicoLiveCommentReceiverException(e);
                }
                catch (HttpRequestException e)
                {
                    throw new NetworkNicoLiveCommentReceiverException(e);
                }
                var playerStatus = await JsonDocument.ParseAsync(str, cancellationToken: cancellationToken).ConfigureAwait(false);
                var playerStatusRoot = playerStatus.RootElement;
                var msUriStr = playerStatusRoot.GetProperty("data").GetProperty("rooms")[0].GetProperty("webSocketUri").GetString();
                var threadId = playerStatusRoot.GetProperty("data").GetProperty("rooms")[0].GetProperty("threadId").GetString();
                Debug.WriteLine(msUriStr);
                if (threadId == null || msUriStr == null)
                {
                    throw new InvalidPlayerStatusNicoLiveCommentReceiverException(str.ToString());
                }
                var msUri = new Uri(msUriStr);

                using var ws = new ClientWebSocket();

                ws.Options.SetRequestHeader("User-Agent", ua);
                ws.Options.AddSubProtocol("msg.nicovideo.jp#json");

                await ws.ConnectAsync(msUri, cancellationToken);

                var sendThread = "[{\"ping\":{\"content\":\"rs:0\"}},{\"ping\":{\"content\":\"ps:0\"}},{\"thread\":{\"thread\":\""+ threadId + "\",\"version\":\"20061206\",\"fork\":0,\"user_id\":\"guest\",\"res_from\":-150,\"with_global\":1,\"scores\":1,\"nicoru\":0}},{\"ping\":{\"content\":\"pf:0\"}},{\"ping\":{\"content\":\"rf:0\"}}]";
                Debug.WriteLine(sendThread);
                try
                {
                    byte[] bodyEncoded = Encoding.UTF8.GetBytes(sendThread);
                    var segment = new ArraySegment<byte>(bodyEncoded);
                    await ws.SendAsync(segment, WebSocketMessageType.Text, true, cancellationToken);
                }
                catch (Exception e) when (e is ObjectDisposedException || e is WebSocketException || e is IOException)
                {
                    if (cancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(null, e, cancellationToken);
                    if (e is ObjectDisposedException)
                        throw;
                    else
                        throw new NetworkNicoLiveCommentReceiverException(e);
                }

                
                var buffer = new byte[2048];
                //コメント受信ループ
                while (true)
                {
                    if (ws.State != WebSocketState.Open)
                        break;
                    var segment = new ArraySegment<byte>(buffer);
                    var result = await ws.ReceiveAsync(segment, cancellationToken);
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Debug.WriteLine(message);
                    parser.Push(message);
                    while(parser.DataAvailable())
                        yield return parser.Pop();
                }
            }
            throw new ConnectionClosedNicoLiveCommentReceiverException();
        }

        public void Dispose()
        {
            httpClient.Dispose();
        }

        private readonly HttpClient httpClient;
        private readonly NiconicoCommentJsonParser parser = new NiconicoCommentJsonParser();
    }
}
