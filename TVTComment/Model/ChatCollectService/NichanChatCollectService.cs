using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Drawing;
using System.Net;

namespace TVTComment.Model.ChatCollectService
{
    class NichanChatCollectService:OnceASecondChatCollectService
    {
        private class ThreadTitleAndResCount
        {
            public string ThreadTitle { get; set; } = null;
            public int ResCount { get; set; } = 0;
        }

        public override string Name => "2ch";
        public override ChatCollectServiceEntry.IChatCollectServiceEntry ServiceEntry { get; }
        public override string GetInformationText()
        {
            lock (threads)
            {
                if (threads.Count == 0)
                    return "対応スレがありません";
                else
                    return string.Join("\n", threads.Select(pair => $"{pair.Value.ThreadTitle ?? "[スレタイ不明]"}  ({pair.Value.ResCount})  {pair.Key}"));
            }
        }

        private Color chatColor;
        private TimeSpan resCollectInterval, threadSearchInterval;
        private NichanUtils.INichanThreadSelector threadSelector;

        private Task resCollectTask;
        private CancellationTokenSource cancel=new CancellationTokenSource();

        /// <summary>
        /// <see cref="ChatCollectException"/>を送出したか
        /// </summary>
        private bool errored=false;

        private ChannelInfo currentChannel;
        private DateTime? currentTime;

        private ConcurrentQueue<Chat> chats = new ConcurrentQueue<Chat>();
        /// <summary>
        /// キーはスレッドのURI
        /// </summary>
        private Dictionary<string, ThreadTitleAndResCount> threads = new Dictionary<string, ThreadTitleAndResCount>();

        /// <summary>
        /// <see cref="NichanChatCollectService"/>を初期化する
        /// </summary>
        /// <param name="serviceEntry">自分を生んだ<seealso cref="IChatCollectServiceEntry"/></param>
        /// <param name="chatColor">コメント色 <seealso cref="Color.Empty"/>ならランダム色</param>
        /// <param name="resCollectInterval">レスを集める間隔 1秒未満にしても1秒になる</param>
        /// <param name="threadSearchInterval">レスを集めるスレッドのリストを更新する間隔 <paramref name="resCollectInterval"/>未満にしても同じ間隔になる</param>
        /// <param name="threadSelector">レスを集めるスレッドを決める<seealso cref="NichanUtils.INichanThreadSelector"/></param>
        public NichanChatCollectService(ChatCollectServiceEntry.IChatCollectServiceEntry serviceEntry,Color chatColor,TimeSpan resCollectInterval,TimeSpan threadSearchInterval,NichanUtils.INichanThreadSelector threadSelector):base(TimeSpan.FromSeconds(10))
        {
            ServiceEntry = serviceEntry;
            this.chatColor = chatColor;

            if (resCollectInterval < TimeSpan.FromSeconds(1))
                resCollectInterval = TimeSpan.FromSeconds(1);
            if (threadSearchInterval < resCollectInterval)
                threadSearchInterval = resCollectInterval;
            this.resCollectInterval = resCollectInterval;
            this.threadSearchInterval = threadSearchInterval;

            this.threadSelector = threadSelector;

            resCollectTask = Task.Run(() => resCollectLoop(cancel.Token),cancel.Token);
        }

        private async Task resCollectLoop(CancellationToken cancellationToken)
        {
            int webExceptionCount = 0;
            int count = (int)(threadSearchInterval.TotalMilliseconds / resCollectInterval.TotalMilliseconds);
            for (int i = count; !cancellationToken.IsCancellationRequested; i++)
            {
                try
                {
                    if (i == count)
                    {
                        //スレ一覧を更新する
                        i = 0;
                        if (currentChannel != null && currentTime != null)
                        {
                            lock (threads)
                            {
                                IEnumerable<string> threadUris = threadSelector.Get(currentChannel, currentTime.Value);

                                //新しいスレ一覧に入ってないものを消す
                                foreach (string uri in threads.Keys.Where(x => !threadUris.Contains(x)).ToList())
                                    threads.Remove(uri);
                                //新しいスレ一覧で追加されたものを追加する
                                foreach (string uri in threadUris)
                                    if (!threads.ContainsKey(uri))
                                        threads.Add(uri, new ThreadTitleAndResCount());
                            }
                        }
                        else
                            i = count - 1;//取得対象のチャンネル、時刻が設定されていなければやり直す
                    }

                    lock (threads)
                    {
                        foreach (var pair in threads)
                        {
                            Nichan.Thread thread = Nichan.ThreadParser.ParseFromUri(pair.Key);
                            int fromResIdx=thread.Res.FindLastIndex(res=>res.Number<=pair.Value.ResCount);
                            fromResIdx++;
                            
                            for(int resIdx=fromResIdx;resIdx<thread.Res.Count;resIdx++)
                            {
                                //1001から先のレスは返さない
                                if (thread.Res[resIdx].Number > 1000)
                                    break;
                                foreach (XElement elem in thread.Res[resIdx].Text.Elements("br").ToArray())
                                {
                                    elem.ReplaceWith("\n");
                                }
                                chats.Enqueue(new Chat(thread.Res[resIdx].Date.Value, thread.Res[resIdx].Text.Value, Chat.PositionType.Normal, Chat.SizeType.Normal,
                                    chatColor.IsEmpty ? Color.White : chatColor, thread.Res[resIdx].UserId,thread.Res[resIdx].Number));
                            }

                            if (pair.Value.ResCount == 0)
                                pair.Value.ThreadTitle = thread.Title;
                            pair.Value.ResCount = thread.Res[thread.Res.Count - 1].Number;
                        }
                    }
                    await Task.Delay(1000, cancellationToken);
                }
                catch(WebException)
                {
                    webExceptionCount++;
                    if (webExceptionCount < 10)
                        continue;
                    else
                        throw;
                }
            }

        }

        protected override IEnumerable<Chat> GetOnceASecond(ChannelInfo channel, DateTime time)
        {
            currentChannel = channel;
            currentTime = time;

            AggregateException e = resCollectTask.Exception;
            if(e!=null)
            {
                errored = true;
                if (e.InnerExceptions.All(innerE => innerE is WebException))
                    throw new ChatCollectException("サーバーとの通信でエラーが発生しました", e);
                else
                    throw new ChatCollectException($"スレ巡回スレッド内で予期しないエラー発生\n\n{e.ToString()}",e);
            }

            var ret = new List<Chat>(chats.Where(x => (time - x.Time).Duration() < TimeSpan.FromSeconds(30)));//投稿から30秒以内のレスのみ返す
            chats = new ConcurrentQueue<Chat>();
            return ret;
        }

        public override void Dispose()
        {
            cancel.Cancel();
            try
            {
                resCollectTask.Wait();
            }
            catch(AggregateException e)when(e.InnerExceptions.All(innerE=>innerE is OperationCanceledException))
            { }
            catch(AggregateException)
            {
                if (!errored) throw;
            }
        }
    }
}
