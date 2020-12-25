using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace TVTComment.Model.ChatCollectService
{
    class PastNichanChatCollectService : OnceASecondChatCollectService
    {
        public override string Name => "2ch過去ログ";

        public override string GetInformationText()
        {
            string ret = $"板: {(this.board == "" ? "[対応板なし]" : this.board)}, 前回の取得時刻: {this.lastCollectTime}";
            lock(this.threadList)
            {
                string threadInformation = string.Join("\n", this.threadList.Select(x => $"{x.Title}  ({x.ResCount})  {x.Uri}"));
                if (threadInformation != "")
                    ret += "\n" + threadInformation;
                else
                    ret += "\n[スレなし]";
            }
            return ret;
        }

        public override ChatCollectServiceEntry.IChatCollectServiceEntry ServiceEntry { get; }

        public PastNichanChatCollectService(
            ChatCollectServiceEntry.IChatCollectServiceEntry chatCollectServiceEntry,
            NichanUtils.INichanBoardSelector boardSelector,
            TimeSpan backTime
        ) : base(TimeSpan.FromSeconds(10))
        {
            this.ServiceEntry = chatCollectServiceEntry;
            this.boardSelector = boardSelector;
            this.backTime = backTime;
        }

        protected override IEnumerable<Chat> GetOnceASecond(ChannelInfo channel, DateTime time)
        {
            var currentTime = new DateTimeOffset(time, TimeSpan.FromHours(9));
            string boardUrl = this.boardSelector.Get(channel, time);
            (string server, string board) = getServerAndBoardFromBoardUrl(boardUrl);

            // 前回の取得から14分以上経っている（未来へのシークを含む）か、
            // 前回の取得時刻を超えて過去にシークしたか、板が違うなら再収集
            if (
                time >= this.lastCollectTime + TimeSpan.FromMinutes(14) ||
                time - this.lastCollectTime < TimeSpan.Zero ||
                board != this.board)
            {

                this.collectChatTaskCancellation?.Cancel();
                try
                {
                    // チャンネル変更やシークでなければcollectChatTaskはとっくに終わってるはず
                    this.collectChatTask?.Wait();
                }
                catch(AggregateException e)
                {
                    collectChatTaskExceptionHandler(e);
                }

                if (this.board != board)
                {
                    this.board = board;
                    lock (this.threadList)
                        this.threadList.Clear();
                }

                if(board != "")
                {
                    this.collectChatTaskCancellation = new CancellationTokenSource();

                    // timeから15分後までに存在したスレのレスを別スレッドで収集開始
                    this.collectChatTask = Task.Run(
                        () => collectChat(board, server, currentTime, currentTime + TimeSpan.FromMinutes(15), this.collectChatTaskCancellation.Token)
                    );
                    this.lastCollectTime = time;
                }
                else
                {
                    this.collectChatTaskCancellation = null;
                    this.collectChatTask = null;
                }
            }

            if(this.collectChatTask?.IsCompleted ?? false)
            {
                // collectChatTaskで例外が出た場合になるべく早く気づけるようにするため
                try
                {
                    this.collectChatTask.Wait();
                }
                catch(AggregateException e)
                {
                    collectChatTaskExceptionHandler(e);
                }
            }

            bool leaped = time < this.lastTime || time > this.lastTime + this.continuousCallLimit;
            var lastTime = !leaped ? this.lastTime : time - TimeSpan.FromSeconds(1);
            lock(this.threadList)
            {
                return this.threadList.SelectMany(x => x.Res).Where(
                    x => lastTime <= x.Date && x.Date < time
                ).Select(x => {
                    foreach (var elem in x.Text.Descendants("br").ToArray())
                    {
                        elem.ReplaceWith("\n");
                    }

                    return new Chat(
                        x.Date.Value, x.Text.Value, Chat.PositionType.Normal, Chat.SizeType.Normal,
                        Color.White, x.UserId, x.Number
                    );
                });
            }
        }

        public override void Dispose()
        {
            this.collectChatTaskCancellation?.Cancel();
            try
            {
                this.collectChatTask?.Wait();
            }
            catch (AggregateException e)
            {
                if (!e.InnerExceptions.All(x => x is OperationCanceledException))
                    throw;
            }
        }

        private async Task collectChat(string board, string oneOfTheServer, DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken)
        {
            if(!this.pastThreadListerCache.TryGetValue(board, out var threadLister))
            {
                threadLister = new Nichan.PastThreadLister(board, oneOfTheServer, this.backTime);
                await threadLister.Initialize(cancellationToken);
                this.pastThreadListerCache.Add(board, threadLister);
            }

            IEnumerable<Nichan.Thread> threads = await threadLister.GetBetween(startTime, endTime, cancellationToken).ConfigureAwait(false);

            IEnumerable<string> currentThreadUrls;
            lock (this.threadList)
                currentThreadUrls = this.threadList.Select(x => x.Uri.ToString()).ToArray();
            IEnumerable<Nichan.Thread> newThreads = threads.Where(x => !currentThreadUrls.Contains(x.Uri.ToString()));

            foreach(var newThread in newThreads)
            {
                string response;
                try
                {
                    response = await httpClient.GetStringAsync(newThread.Uri.ToString(), cancellationToken).ConfigureAwait(false);
                }
                catch (HttpRequestException e)
                {
                    if (e.StatusCode == null)
                        throw new Nichan.NetworkException(newThread.Uri.ToString(), null, e);
                    else
                        throw new Nichan.HttpErrorResponseException((int)e.StatusCode.Value, null, newThread.Uri.ToString(), null, e);
                }

                using var textReader = new StringReader(response);
                Nichan.Thread thread = Nichan.ThreadParser.ParseFromStream(textReader);
                thread.Uri = newThread.Uri;

                lock(this.threadList)
                    this.threadList.Add(thread);
            }
        }

        private static readonly Regex reBoardUrl = new Regex(@"//(?<server>[^.]*)\.5ch\.net/(?<board>[^/]*)(^|/.*)");
        private static (string server, string board) getServerAndBoardFromBoardUrl(string boardUrl)
        {
            Match match = reBoardUrl.Match(boardUrl);
            return (match.Groups["server"].Value, match.Groups["board"].Value);
        }

        /// <summary>
        /// <see cref="collectChatTask"/>を<see cref="Task.Wait"/>した時に出た<see cref="AggregateException"/>を適切な例外に変換して投げなおす。
        /// 例外を投げなければタスクがキャンセルされたことによる例外だったということ。
        /// </summary>
        /// <param name="e"></param>
        private static void collectChatTaskExceptionHandler(AggregateException e)
        {
            if (e.InnerExceptions.All(x => x is OperationCanceledException))
                return;
            if (e.InnerExceptions.Count != 1)
                ExceptionDispatchInfo.Capture(e).Throw(); // スタックトレースを保って再スロー

            switch (e.InnerExceptions[0])
            {
                case Nichan.HttpErrorResponseException http:
                    throw new ChatCollectException($"HTTP でエラーのステータスコードが返りました\nコード: {http.HttpStatusCode}\nURL: {http.Url}", e);
                case Nichan.ErrorResponseException errorResponse:
                    throw new ChatCollectException($"エラー応答が返りました\nURL: {errorResponse.Url}", e);
                case Nichan.InvalidResponseException invalidResponse:
                    throw new ChatCollectException($"不正な応答が返りました\nURL: {invalidResponse.Url}", e);
                case Nichan.ResponseException response:
                    throw new ChatCollectException($"応答エラーです\nURL: {response.Url}", e);
                case Nichan.NetworkException network:
                    throw new ChatCollectException($"接続できませんでした\nURL: {network.Url}", e);
                case Nichan.CommunicationException communication:
                    throw new ChatCollectException($"通信エラーです\nURL: {communication.Url}", e);
            }

            ExceptionDispatchInfo.Capture(e).Throw(); // スタックトレースを保って再スロー
        }

        private static readonly HttpClient httpClient = new HttpClient();
        private readonly NichanUtils.INichanBoardSelector boardSelector;
        private readonly Dictionary<string, Nichan.PastThreadLister> pastThreadListerCache = new Dictionary<string, Nichan.PastThreadLister>();
        private readonly List<Nichan.Thread> threadList = new List<Nichan.Thread>();
        private string board = "";
        private DateTimeOffset lastCollectTime = DateTimeOffset.MinValue;
        private Task collectChatTask = null;
        private CancellationTokenSource collectChatTaskCancellation = null;
        private readonly TimeSpan backTime;
    }
}
