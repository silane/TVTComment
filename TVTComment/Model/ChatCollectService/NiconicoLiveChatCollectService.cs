using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
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

        private readonly string originalLiveId;
        private string liveId = "";

        private readonly HttpClient httpClient;
        private readonly Task chatCollectTask;
        private readonly ConcurrentQueue<NiconicoUtils.NiconicoCommentXmlTag> commentTagQueue = new ConcurrentQueue<NiconicoUtils.NiconicoCommentXmlTag>();
        private readonly NiconicoUtils.NicoLiveCommentReceiver commentReceiver;
        private readonly NiconicoUtils.NicoLiveCommentSender commentSender;
        private DateTime lastHeartbeatTime = DateTime.MinValue;
        private readonly CancellationTokenSource cancel = new CancellationTokenSource();

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

            this.commentReceiver = new NiconicoUtils.NicoLiveCommentReceiver(session);
            this.commentSender = new NiconicoUtils.NicoLiveCommentSender(session);

            this.chatCollectTask = this.collectChat(this.cancel.Token);
        }

        public string GetInformationText()
        {
            return $"生放送ID: {this.originalLiveId}" + (this.liveId != "" ? $" ({this.liveId})" : "");
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
                    e is ChatReceivingException && e.InnerException == null ? e.Message : $"コメント取得でエラーが発生: {e}",
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
            while(this.commentTagQueue.TryDequeue(out var tag))
            {
                var chatTag = tag as NiconicoUtils.ChatNiconicoCommentXmlTag;
                var leaveThreadTag = tag as NiconicoUtils.LeaveThreadNiconicoCommentXmlTag;
                if (chatTag != null)
                    ret.Add(NiconicoUtils.ChatNiconicoCommentXmlTagToChat.Convert(chatTag));
                else if (leaveThreadTag != null)
                    cancel.Cancel();
            }
            return ret;
        }

        private async Task collectChat(CancellationToken cancel)
        {
            string playerStatusStr;
            try
            {
                playerStatusStr = await httpClient.GetStringAsync($"http://live.nicovideo.jp/api/getplayerstatus/{this.originalLiveId}").ConfigureAwait(false);
            }
            catch(HttpRequestException e)
            {
                throw new ChatReceivingException("サーバーとの通信でエラーが発生しました", e);
            }
            var playerStatus = XDocument.Parse(playerStatusStr).Root;

            if (playerStatus.Attribute("status").Value != "ok")
            {
                if (playerStatus.Element("error")?.Element("code")?.Value == "comingsoon")
                    throw new ChatReceivingException("放送開始前です");
                if (playerStatus.Element("error")?.Element("code")?.Value == "closed")
                    throw new ChatReceivingException("放送終了後です");
                throw new ChatReceivingException("コメントサーバーから予期しないPlayerStatusが返されました:\n" + playerStatusStr);
            }

            this.liveId = playerStatus.Element("stream").Element("id").Value;

            try
            {
                await foreach (NiconicoUtils.NiconicoCommentXmlTag tag in this.commentReceiver.Receive(this.liveId, cancel))
                {
                    this.commentTagQueue.Enqueue(tag);
                }
            }
            catch(NiconicoUtils.InvalidPlayerStatusNicoLiveCommentReceiverException e)
            {
                throw new ChatReceivingException("サーバーから予期しないPlayerStatusが返されました:\n" + e.PlayerStatus, e);
            }
            catch(NiconicoUtils.NetworkNicoLiveCommentReceiverException e)
            {
                throw new ChatReceivingException("サーバーとの通信でエラーが発生しました", e);
            }
            catch(NiconicoUtils.ConnectionClosedNicoLiveCommentReceiverException e)
            {
                throw new ChatReceivingException("サーバーとの通信が切断されました", e);
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
            using (this.commentReceiver)
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
    }
}
