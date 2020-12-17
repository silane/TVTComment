using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TVTComment.Model.ChatCollectService
{
    class NiconicoLiveChatCollectService : IChatCollectService
    {
        public class ChatPostObject : BasicChatPostObject
        {
            public string Mail { get; }
            public ChatPostObject(string text, string mail) : base(text)
            {
                Mail = mail;
            }
        }

        public string Name => "ニコニコ生放送";
        public ChatCollectServiceEntry.IChatCollectServiceEntry ServiceEntry { get; }
        public bool CanPost => true;


        [Serializable]
        private class ChatReceivingException : Exception
        {
            public ChatReceivingException() { }
            public ChatReceivingException(string message) : base(message) { }
            public ChatReceivingException(string message, Exception inner) : base(message, inner) { }
            protected ChatReceivingException(
              System.Runtime.Serialization.SerializationInfo info,
              System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        }

        private string originalLiveId;
        private string liveId = "";
        private NiconicoUtils.NiconicoCommentXmlParser parser = new NiconicoUtils.NiconicoCommentXmlParser(true);
        private NetworkStream socketStream;
        private HttpClient httpClient;
        private CancellationTokenSource cancel = new CancellationTokenSource();
        private Task chatCollectTask;
        private DateTime lastHeartbeatTime = DateTime.MinValue;
        private NiconicoUtils.NicoLiveCommentSender commentSender;
        private static readonly Encoding utf8Encoding = new UTF8Encoding(false);

        public NiconicoLiveChatCollectService(
            ChatCollectServiceEntry.IChatCollectServiceEntry serviceEntry, string liveId,
            NiconicoUtils.NiconicoLoginSession session
        )
        {
            this.ServiceEntry = serviceEntry;
            this.originalLiveId = liveId;

            var assembly = Assembly.GetExecutingAssembly().GetName();
            var ua = assembly.Name + "/" + assembly.Version.ToString(3);

            var handler = new HttpClientHandler();
            handler.CookieContainer.Add(session.Cookie);
            this.httpClient = new HttpClient(handler);
            this.httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", ua);

            this.commentSender = new NiconicoUtils.NicoLiveCommentSender(session);

            this.chatCollectTask = this.collectChat(this.cancel.Token);
        }

        public string GetInformationText()
        {
            return $"生放送ID: {this.originalLiveId} - {(socketStream == null ?  "サーバーに未接続" : "サーバーに接続中")}";
        }

        public IEnumerable<Chat> GetChats(ChannelInfo channel, DateTime time)
        {
            if (chatCollectTask.IsCanceled)
            {
                throw new ChatCollectException("放送が終了しました");
            }
            if (chatCollectTask.IsFaulted)
            {
                //非同期部分で例外発生
                var e = chatCollectTask.Exception.InnerExceptions.Count == 1 ? chatCollectTask.Exception.InnerExceptions[0] : chatCollectTask.Exception;
                throw new ChatCollectException(
                    e is ChatReceivingException ? e.Message : $"コメント取得で予期しないエラーが発生: {e}",
                    chatCollectTask.Exception
                );
            }

            // Heartbeat送信
            if(DateTime.Now >= this.lastHeartbeatTime.AddSeconds(60))
            {
                this.lastHeartbeatTime = DateTime.Now;
                this.heartbeat(this.cancel.Token);
            }

            //非同期部分で集めたデータからチャットを生成
            var ret = new List<Chat>();
            lock (parser)
            {
                while (parser.DataAvailable())
                {
                    var tag = parser.Pop();
                    var chatTag = tag as NiconicoUtils.ChatNiconicoCommentXmlTag;
                    var leaveThreadTag = tag as NiconicoUtils.LeaveThreadNiconicoCommentXmlTag;
                    if (chatTag != null)
                        ret.Add(NiconicoUtils.ChatNiconicoCommentXmlTagToChat.Convert(chatTag));
                    else if (leaveThreadTag != null)
                        cancel.Cancel();
                }
            }
            return ret;
        }

        private async Task collectChat(CancellationToken cancel)
        {
            try
            {
                cancel.Register(() =>
                {
                    httpClient.CancelPendingRequests();
                    socketStream?.Dispose();//ReadAsyncはキャンセルしてもすぐに戻らないので無理やり切る
                    socketStream = null;
                });

                for (int disconnectedCount = 0; ; disconnectedCount++)
                {
                    string str = await httpClient.GetStringAsync($"http://live.nicovideo.jp/api/getplayerstatus/{this.originalLiveId}").ConfigureAwait(false);
                    var playerStatus = XDocument.Parse(str).Root;

                    if (playerStatus.Attribute("status").Value != "ok")
                    {
                        if (playerStatus.Element("error")?.Element("code")?.Value == "comingsoon")
                            throw new ChatReceivingException("放送開始前です");
                        if (playerStatus.Element("error")?.Element("code")?.Value == "closed")
                            throw new ChatReceivingException("放送終了後です");
                        throw new ChatReceivingException("コメントサーバーから予期しないエラーが返されました:\n" + str);
                    }

                    this.liveId = playerStatus.Element("stream").Element("id").Value;
                    string threadId = playerStatus.Element("ms").Element("thread").Value;
                    string ms = playerStatus.Element("ms").Element("addr").Value;
                    string msPort = playerStatus.Element("ms").Element("port").Value;

                    string body = $"<thread res_from=\"-10\" version=\"20061206\" thread=\"{threadId}\" scores=\"1\" />\0";
                    using (TcpClient tcpClinet = new TcpClient(ms, int.Parse(msPort)))
                    {
                        try
                        {
                            socketStream = tcpClinet.GetStream();

                            byte[] body_encoded = utf8Encoding.GetBytes(body);
                            await socketStream.WriteAsync(body_encoded, 0, body_encoded.Length, cancel).ConfigureAwait(false);

                            //コメント受信ループ
                            while (true)
                            {
                                byte[] buf = new byte[2048];
                                int receivedByte = await socketStream.ReadAsync(buf, 0, buf.Length, cancel).ConfigureAwait(false);
                                if (receivedByte == 0)
                                    throw new ChatReceivingException("コメントサーバーとの通信が切断されました");

                                lock (parser)
                                    parser.Push(utf8Encoding.GetString(buf, 0, receivedByte));
                            }
                        }
                        catch (ChatReceivingException)
                        {
                            if (disconnectedCount >= 3)
                                throw;
                            else
                                continue;
                        }
                        finally
                        {
                            socketStream?.Dispose();
                            socketStream = null;
                        }
                    }
                }
            }
            catch (ObjectDisposedException e)
            {
                if (cancel.IsCancellationRequested)
                    throw new OperationCanceledException("Receive loop was canceled", e, cancel);
                throw;
            }
            catch (Exception e) when (e is HttpRequestException || e is SocketException || e is IOException)
            {
                if (cancel.IsCancellationRequested)
                    throw new OperationCanceledException("Receive loop was canceled", e, cancel);
                throw new ChatReceivingException("コメントサーバーとの通信でエラーが発生しました", e);
            }
        }

        private async void heartbeat(CancellationToken cancel)
        {
            if (this.liveId == "")
                return;
            // async void なのでこの関数内の例外は無視される
            await this.httpClient.PostAsync(
                "http://ow.live.nicovideo.jp/api/heartbeat",
                new FormUrlEncodedContent(new Dictionary<string, string> { { "v", this.liveId } }),
                cancel
            );
        }

        public async Task PostChat(BasicChatPostObject chatPostObject)
        {
            if (this.liveId == "")
                throw new ChatPostException("コメントが投稿できる状態にありません。しばらく待ってから再試行してください。");

            try
            {
                await this.commentSender.Send(this.liveId, chatPostObject.Text, (chatPostObject as ChatPostObject)?.Mail ?? "");
            }
            catch (NiconicoUtils.NetworkNicoLiveCommentSenderException e)
            {
                throw new ChatPostException($"サーバーに接続できませんでした", e);
            }
            catch (NiconicoUtils.InvalidPlayerStatusNicoLiveCommentSenderException e)
            {
                throw new ChatPostException($"サーバーから無効な PlayerStatus が返されました\n\n{e.PlayerStatus}", e);
            }
            catch(NiconicoUtils.ResponseFormatNicoLiveCommentSenderException e)
            {
                throw new ChatPostException($"サーバーから予期しない形式の応答がありました\n\n{e.Response}", e);
            }
            catch(NiconicoUtils.ResponseErrorNicoLiveCommentSenderException e)
            {
                throw new ChatPostException($"サーバーからエラーが返されました", e);
            }
        }

        public void Dispose()
        {
            using (this.commentSender)
            using (this.httpClient)
            {
                cancel.Cancel();
                try
                {
                    chatCollectTask.Wait();
                }
                //Waitからの例外がタスクがキャンセルされたことによるものか、通信エラーなら無視
                catch (AggregateException e) when (e.InnerExceptions.All(innerE => innerE is OperationCanceledException || innerE is ChatReceivingException))
                {
                }
            }
        }

        /// <summary>
        /// マシンのロケール設定に関係なく今の日本標準時を返す
        /// </summary>
        private static DateTime getDateTimeJstNow()
        {
            return DateTime.SpecifyKind(DateTime.UtcNow.AddHours(9), DateTimeKind.Unspecified);
        }
    }
}
