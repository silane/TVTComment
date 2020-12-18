using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TVTComment.Model.ChatCollectService
{
    class NewNiconicoJikkyouChatCollectService : IChatCollectService
    {
        public class ChatPostObject : BasicChatPostObject
        {
            public string Mail { get; }
            public ChatPostObject(string text, string mail) : base(text)
            {
                Mail = mail;
            }
        }

        private class ChatReceivingException : Exception
        {
            public ChatReceivingException(string message) : base(message) { }
            public ChatReceivingException(string message, Exception inner) : base(message, inner) { }
        }

        public string Name => "新ニコニコ実況";
        public string GetInformationText()
        {
            return $"生放送ID: {(this.originalLiveId == "" ? "[対応する生放送IDがありません]" : this.originalLiveId)}";

        }
        public ChatCollectServiceEntry.IChatCollectServiceEntry ServiceEntry { get; }
        public bool CanPost => true;

        private readonly NiconicoUtils.LiveIdResolver liveIdResolver;
        private readonly NiconicoUtils.NiconicoLoginSession niconicoLoginSession;
        private readonly HttpClient httpClient;
        private readonly NiconicoUtils.NicoLiveCommentReceiver commentReceiver;
        private readonly NiconicoUtils.NicoLiveCommentSender commentSender;
        private readonly ConcurrentQueue<NiconicoUtils.NiconicoCommentXmlTag> commentTagQueue = new ConcurrentQueue<NiconicoUtils.NiconicoCommentXmlTag>();

        private string originalLiveId;
        private string liveId;
        private Task chatCollectTask = null;
        private CancellationTokenSource cancellationTokenSource = null;
        private DateTime lastHeartbeatTime = DateTime.MinValue;

        public NewNiconicoJikkyouChatCollectService(
            ChatCollectServiceEntry.IChatCollectServiceEntry serviceEntry,
            NiconicoUtils.LiveIdResolver liveIdResolver,
            NiconicoUtils.NiconicoLoginSession niconicoLoginSession
        )
        {
            this.ServiceEntry = serviceEntry;
            this.liveIdResolver = liveIdResolver;
            this.niconicoLoginSession = niconicoLoginSession;

            var assembly = Assembly.GetExecutingAssembly().GetName();
            var ua = assembly.Name + "/" + assembly.Version.ToString(3);

            var handler = new HttpClientHandler();
            handler.CookieContainer.Add(niconicoLoginSession.Cookie);
            this.httpClient = new HttpClient(handler);
            this.httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", ua);

            this.commentReceiver = new NiconicoUtils.NicoLiveCommentReceiver(niconicoLoginSession);
            this.commentSender = new NiconicoUtils.NicoLiveCommentSender(niconicoLoginSession);
        }

        public IEnumerable<Chat> GetChats(ChannelInfo channel, DateTime time)
        {
            if (this.chatCollectTask?.IsFaulted ?? false)
            {
                //非同期部分で例外発生
                throw new ChatCollectException($"コメント取得でエラーが発生: {chatCollectTask.Exception}", chatCollectTask.Exception);
            }

            string originalLiveId = this.liveIdResolver.Resolve(channel.NetworkId, channel.ServiceId);

            if(originalLiveId != this.originalLiveId)
            {
                // 生放送IDが変更になった場合

                this.cancellationTokenSource?.Cancel();
                try
                {
                    chatCollectTask?.Wait();
                }
                //Waitからの例外がタスクがキャンセルされたことによるものか、通信エラー等なら無視
                catch (AggregateException e) when (e.InnerExceptions.All(
                    innerE => innerE is OperationCanceledException || innerE is ChatReceivingException
                ))
                {
                }
                this.originalLiveId = originalLiveId;
                this.commentTagQueue.Clear();

                if(this.originalLiveId != "")
                {
                    this.cancellationTokenSource = new CancellationTokenSource();
                    chatCollectTask = collectChat(originalLiveId, this.cancellationTokenSource.Token);
                }
            }

            if (this.originalLiveId == "")
            {
                return new Chat[0];
            }

            // Heartbeat送信
            if (DateTime.Now >= this.lastHeartbeatTime.AddSeconds(60))
            {
                this.lastHeartbeatTime = DateTime.Now;
                this.heartbeat(this.cancellationTokenSource.Token);
            }

            var ret = new List<Chat>();
            while (this.commentTagQueue.TryDequeue(out var tag))
            {
                switch(tag)
                {
                    case NiconicoUtils.ChatNiconicoCommentXmlTag chatTag:
                        ret.Add(NiconicoUtils.ChatNiconicoCommentXmlTagToChat.Convert(chatTag));
                        break;
                }
            }
            return ret;
        }

        private async Task collectChat(string originalLiveId, CancellationToken cancellationToken)
        {
            string playerStatusStr;
            try
            {
                playerStatusStr = await this.httpClient.GetStringAsync($"http://live.nicovideo.jp/api/getplayerstatus/{originalLiveId}").ConfigureAwait(false);
            }
            catch (HttpRequestException e)
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
                await foreach (NiconicoUtils.NiconicoCommentXmlTag tag in this.commentReceiver.Receive(originalLiveId, cancellationToken))
                {
                    this.commentTagQueue.Enqueue(tag);
                }
            }
            catch (NiconicoUtils.InvalidPlayerStatusNicoLiveCommentReceiverException e)
            {
                throw new ChatReceivingException("サーバーから予期しないPlayerStatusが返されました:\n" + e.PlayerStatus, e);
            }
            catch (NiconicoUtils.NetworkNicoLiveCommentReceiverException e)
            {
                throw new ChatReceivingException("サーバーとの通信でエラーが発生しました", e);
            }
            catch (NiconicoUtils.ConnectionClosedNicoLiveCommentReceiverException e)
            {
                throw new ChatReceivingException("サーバーとの通信が切断されました", e);
            }
        }

        private async void heartbeat(CancellationToken cancel)
        {
            string liveId = this.liveId;
            if (liveId == "")
                return;
            // async void なのでこの関数内の例外は無視される
            await this.httpClient.PostAsync(
                "http://ow.live.nicovideo.jp/api/heartbeat",
                new FormUrlEncodedContent(new Dictionary<string, string> { { "v", liveId } }),
                cancel
            );
        }

        public async Task PostChat(BasicChatPostObject chatPostObject)
        {
            string liveId = this.liveId;
            if (liveId == "")
                throw new ChatPostException("コメントが投稿できる状態にありません。しばらく待ってから再試行してください。");

            try
            {
                await this.commentSender.Send(liveId, chatPostObject.Text, (chatPostObject as ChatPostObject)?.Mail ?? "");
            }
            catch (NiconicoUtils.NetworkNicoLiveCommentSenderException e)
            {
                throw new ChatPostException($"サーバーに接続できませんでした", e);
            }
            catch (NiconicoUtils.InvalidPlayerStatusNicoLiveCommentSenderException e)
            {
                throw new ChatPostException($"サーバーから無効な PlayerStatus が返されました\n\n{e.PlayerStatus}", e);
            }
            catch (NiconicoUtils.ResponseFormatNicoLiveCommentSenderException e)
            {
                throw new ChatPostException($"サーバーから予期しない形式の応答がありました\n\n{e.Response}", e);
            }
            catch (NiconicoUtils.ResponseErrorNicoLiveCommentSenderException e)
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
                this.cancellationTokenSource.Cancel();
                try
                {
                    this.chatCollectTask.Wait();
                }
                //Waitからの例外がタスクがキャンセルされたことによるものか、通信エラー等なら無視
                catch (AggregateException e) when (e.InnerExceptions.All(
                    innerE => innerE is OperationCanceledException || innerE is ChatReceivingException
                ))
                {
                }
            }
        }
    }
}
