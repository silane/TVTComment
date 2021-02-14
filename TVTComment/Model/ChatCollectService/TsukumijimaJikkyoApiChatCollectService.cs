using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TVTComment.Model.ChatCollectService
{
    class TsukumijimaJikkyoApiChatCollectService : OnceASecondChatCollectService
    {
        public override string Name => "非公式ニコニコ実況過去ログ";

        public override string GetInformationText()
        {
            if (this.jkId != 0)
                return $"現在の実況ID: {this.jkId}";
            else
                return $"現在の実況ID: [対応なし]";
        }

        public override ChatCollectServiceEntry.IChatCollectServiceEntry ServiceEntry { get; }
        public override bool CanPost => false;

        public TsukumijimaJikkyoApiChatCollectService(
            ChatCollectServiceEntry.IChatCollectServiceEntry serviceEntry,
            NiconicoUtils.JkIdResolver jkIdResolver
        ) : base(TimeSpan.FromSeconds(10))
        {
            this.ServiceEntry = serviceEntry;
            this.jkIdResolver = jkIdResolver;
        }

        protected override IEnumerable<Chat> GetOnceASecond(ChannelInfo channel, DateTime time)
        {
            int jkId = jkIdResolver.Resolve(channel.NetworkId, channel.ServiceId);

            if(
                jkId != this.jkId ||
                time >= this.lastGetStartTime + TimeSpan.FromMinutes(14) ||
                time < this.lastGetStartTime
            )
            {
                // 前回の取得から14分以上経っている（未来へのシークを含む）か、
                // 前回の取得時刻を超えて過去にシークしたか、実況IDが違うなら再収集
                this.chatCollectTaskCancellation?.Cancel();
                try
                {
                    this.chatCollectTask?.Wait();
                }
                catch(AggregateException e)
                {
                    this.chatCollectTask = null;
                    chatCollectTaskExceptionHandler(e);
                }
                this.chatCollectTask = null;

                if (jkId != this.jkId)
                {
                    lock (this.chats)
                    {
                        this.chats.Clear();
                    }
                }

                if (jkId != 0)
                {
                    this.chatCollectTaskCancellation = new CancellationTokenSource();
                    var startTime = new DateTimeOffset(time, TimeSpan.FromHours(9));
                    var endTime = startTime + TimeSpan.FromMinutes(15);
                    this.chatCollectTask = Task.Run(() => this.collectChat(jkId, startTime, endTime, this.chatCollectTaskCancellation.Token));
                    this.lastGetStartTime = startTime;
                }
                else
                {
                    this.chatCollectTaskCancellation = null;
                    this.chatCollectTask = null;
                }

                this.jkId = jkId;
            }

            try
            {
                this.chatCollectTask?.Wait();
            }
            catch(AggregateException e)
            {
                this.chatCollectTask = null;
                chatCollectTaskExceptionHandler(e);
            }
            this.chatCollectTask = null;

            lock (this.chats)
            {
                IEnumerable<Chat> ret = this.chats.Where(x => time <= x.Chat.Time && x.Chat.Time < time + TimeSpan.FromSeconds(1)).Select(x => x.Chat);
                return ret;
            }
        }

        public override Task PostChat(BasicChatPostObject postObject)
        {
            throw new NotImplementedException();
        }

        public override void Dispose()
        {
            this.chatCollectTaskCancellation?.Cancel();
            try
            {
                this.chatCollectTask?.Wait();
            }
            catch(AggregateException e)
            {
                try
                {
                    chatCollectTaskExceptionHandler(e);
                }
                catch(ChatCollectException)
                { }
            }
            finally
            {
                this.chatCollectTask = null;
            }
        }

        private async Task collectChat(int jkId, DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken)
        {
            string url = $"https://jikkyo.tsukumijima.net/api/kakolog/jk{jkId}?starttime={startTime.ToUnixTimeSeconds()}&endtime={endTime.ToUnixTimeSeconds()}&format=json";

            Stream response;
            try
            {
                response = await httpClient.GetStreamAsync(url, cancellationToken).ConfigureAwait(false);
            }
            catch(HttpRequestException e)
            {
                if (e.StatusCode == null)
                    throw new ChatCollectException($"接続できませんでした\nURL: {url}", e);
                else
                    throw new ChatCollectException($"HTTP でエラーのステータスコードが返りました\nURL: {url}\nコード: {e.StatusCode}", e);
            }

            JsonDocument jsonDocument;
            try
            {
                jsonDocument = await JsonDocument.ParseAsync(response, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch(JsonException e)
            {
                throw new ChatCollectException($"不正な応答が返りました\nURL: {url}", e);
            }

            try
            {
                JsonElement elem;
                string error = jsonDocument.RootElement.TryGetProperty("error", out elem) ? elem.ValueKind == JsonValueKind.String ? elem.GetString() : null : null;
                if (error != null)
                    throw new ChatCollectException($"エラー応答が返されました\nサーバーからのメッセージ: {error}");

                JsonElement packet = jsonDocument.RootElement.GetProperty("packet");

                (string thread, int no)[] threadNoList;
                lock(this.chats)
                    threadNoList = this.chats.Select(x => (x.Thread, x.Chat.Number)).ToArray();

                for (int i = 0; i < packet.GetArrayLength(); ++i)
                {
                    JsonElement chatObj = packet[i].GetProperty("chat");
                    string thread = chatObj.GetProperty("thread").GetString();
                    int no = int.Parse(chatObj.GetProperty("no").GetString());

                    if (threadNoList.Contains((thread, no))) // 同じスレッドの同じコメ番があればスキップ
                        continue;
                    
                    if (chatObj.TryGetProperty("deleted", out _)) //削除済みの場合スキップ
                        continue;

                    int vpos = int.Parse(chatObj.GetProperty("vpos").GetString());
                    long date = int.Parse(chatObj.GetProperty("date").GetString());
                    int dateUsec = chatObj.TryGetProperty("date_usec", out elem) ? elem.ValueKind == JsonValueKind.String ? int.Parse(elem.GetString()) : 0 : 0;
                    string mail = chatObj.TryGetProperty("mail", out elem) ? elem.ValueKind == JsonValueKind.String ? elem.GetString() : "" : "";
                    string userId = chatObj.GetProperty("user_id").GetString();
                    int premium = chatObj.TryGetProperty("premium", out elem) ? elem.ValueKind == JsonValueKind.String ? int.Parse(elem.GetString()) : 0 : 0;
                    int anonymity = chatObj.TryGetProperty("anonymity", out elem) ? elem.ValueKind == JsonValueKind.String ? int.Parse(elem.GetString()) : 0 : 0;
                    int abone = chatObj.TryGetProperty("abone", out elem) ? elem.ValueKind == JsonValueKind.String ? int.Parse(elem.GetString()) : 0 : 0;
                    string content = chatObj.GetProperty("content").GetString();

                    var chatTag = new NiconicoUtils.ChatNiconicoCommentXmlTag(
                        content, 0, no, vpos, date, dateUsec, mail, userId, premium, anonymity, abone
                    );

                    lock (this.chats)
                    {
                        this.chats.Add((thread, NiconicoUtils.ChatNiconicoCommentXmlTagToChat.Convert(chatTag)));
                    }
                }
            }
            catch (Exception e) when (
                e is InvalidOperationException || e is KeyNotFoundException || e is FormatException
            )
            {
                throw new ChatCollectException("不正な応答が返りました", e);
            }
            finally
            {
                jsonDocument.Dispose();
            }
        }

        private static void chatCollectTaskExceptionHandler(AggregateException e)
        {
            if (e.InnerExceptions.All(x => x is OperationCanceledException))
                return;
            if (e.InnerExceptions.Count != 1)
                ExceptionDispatchInfo.Capture(e).Throw(); // スタックトレースを保って再スロー

            switch (e.InnerExceptions[0])
            {
                case ChatCollectException chatCollectException:
                    throw chatCollectException;
            }

            ExceptionDispatchInfo.Capture(e).Throw(); // スタックトレースを保って再スロー
        }

        private readonly NiconicoUtils.JkIdResolver jkIdResolver;
        private readonly List<(string Thread, Chat Chat)> chats = new List<(string, Chat)>();
        private int jkId = 0;
        /// <summary>
        /// この時刻から15分後までが前回の取得時間
        /// </summary>
        private DateTimeOffset lastGetStartTime = DateTimeOffset.MinValue;
        private Task chatCollectTask = null;
        private CancellationTokenSource chatCollectTaskCancellation = null;
        private static readonly HttpClient httpClient = new HttpClient();

        static TsukumijimaJikkyoApiChatCollectService()
        {
            var assembly = Assembly.GetExecutingAssembly().GetName();
            string version = assembly.Version.ToString(3);

            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", $"TvtComment/{version}");
        }
    }
}
