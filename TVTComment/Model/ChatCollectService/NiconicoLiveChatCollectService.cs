using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

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
            Stream playerStatusStr;
            try
            {
                if (!originalLiveId.StartsWith("lv")) // 代替えAPIではコミュニティ・チャンネルにおけるコメント鯖取得ができないのでlvを取得しに行く
                {
                    var getLiveId = await httpClient.GetStreamAsync($"https://live2.nicovideo.jp/unama/tool/v1/broadcasters/social_group/{originalLiveId}/program").ConfigureAwait(false);
                    var liveIdJson = await JsonDocument.ParseAsync(getLiveId, cancellationToken: cancel).ConfigureAwait(false);
                    var liveIdRoot = liveIdJson.RootElement;
                    if (!liveIdRoot.GetProperty("meta").GetProperty("errorCode").GetString().Equals("OK")) throw new ChatReceivingException("コミュニティ・チャンネルが見つかりませんでした");
                    originalLiveId = liveIdRoot.GetProperty("data").GetProperty("nicoliveProgramId").GetString(); // lvから始まるLiveIDに置き換え

                }
                playerStatusStr = await httpClient.GetStreamAsync($"https://live2.nicovideo.jp/unama/watch/{originalLiveId}/programinfo").ConfigureAwait(false);
            }
            catch(HttpRequestException e)
            {
                throw new ChatReceivingException("サーバーとの通信でエラーが発生しました", e);
            }
            var playerStatus = await JsonDocument.ParseAsync(playerStatusStr, cancellationToken: cancel).ConfigureAwait(false);
            var playerStatusRoot = playerStatus.RootElement;

            if (!playerStatusRoot.GetProperty("meta").GetProperty("errorCode").GetString().Equals("OK"))
            {
                if (playerStatusRoot.GetProperty("meta").GetProperty("errorCode").GetString().Equals("SERVER_ERROR"))
                    throw new ChatReceivingException("ニコニコのサーバがメンテナンス中の可能性があります");
                if (playerStatusRoot.GetProperty("meta").GetProperty("errorCode").GetString().Equals("INTERNAL_SERVER_ERROR"))
                    throw new ChatReceivingException("ニコニコのサーバで内部エラーが発生しました");
                if (playerStatusRoot.GetProperty("meta").GetProperty("errorCode").GetString().Equals("NOT_FOUND"))
                    throw new ChatReceivingException("放送が見つかりませんでした");
                throw new ChatReceivingException("コメントサーバーから予期しないPlayerStatusが返されました:\n" + playerStatusStr);
            }

            liveId = playerStatusRoot.GetProperty("data").GetProperty("socialGroup").GetProperty("id").GetString();

            try
            {
                await foreach (NiconicoUtils.NiconicoCommentXmlTag tag in this.commentReceiver.Receive(originalLiveId, cancel))
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
                throw new ChatReceivingException("サーバーとの通信でエラーが発生しました"+ liveId, e);
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
