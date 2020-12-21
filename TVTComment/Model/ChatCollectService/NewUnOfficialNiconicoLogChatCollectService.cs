﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Windows;

namespace TVTComment.Model.ChatCollectService
{
    class NewUnOfficialNiconicoLogChatCollectService : OnceASecondChatCollectService
    {
        [Serializable]
        private class ServerErrorException : Exception
        {
            public ServerErrorException() { }
            public ServerErrorException(string message) : base(message) { }
            public ServerErrorException(string message, Exception inner) : base(message, inner) { }
            protected ServerErrorException(
              System.Runtime.Serialization.SerializationInfo info,
              System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        }

        private NiconicoUtils.JkIdResolver jkIdResolver;
        private int lastJkId = 0;
        private DateTime lastGetTime;

        private ConcurrentQueue<NiconicoUtils.ChatAndVpos> chats = new ConcurrentQueue<NiconicoUtils.ChatAndVpos>();
        private Task chatCollectTask;
        private HttpClient client;

        public override string Name => "非公式ニコニコ実況過去ログ";
        public override ChatCollectServiceEntry.IChatCollectServiceEntry ServiceEntry { get; }

        public override string GetInformationText()
        {
            if (lastJkId != 0)
                return $"現在の実況ID: {lastJkId.ToString()}, 前回の取得時刻: {lastGetTime.ToString("HH:mm:ss")}";
            else
                return $"現在の実況ID: [対応なし]";
        }

        public NewUnOfficialNiconicoLogChatCollectService(ChatCollectServiceEntry.IChatCollectServiceEntry serviceEntry, NiconicoUtils.JkIdResolver jkIdResolver) : base(new TimeSpan(0, 0, 10))
        {
            this.ServiceEntry = serviceEntry;
            this.jkIdResolver = jkIdResolver;
            var handler = new HttpClientHandler();
            client = new HttpClient(handler);
        }

        protected override IEnumerable<Chat> GetOnceASecond(ChannelInfo channel, DateTime time)
        {
            List<Chat> ret = new List<Chat>();

            int jkId = jkIdResolver.Resolve(channel.NetworkId, channel.ServiceId);

            if (jkId == 0)
            {
                //対応するjkidがなかった
                lastJkId = 0;
                return ret;
            }

            if (lastJkId == jkId)
            {
                lock (chats)
                {
                    foreach (var chat in chats.Where(chat => time.AddSeconds(2) <= chat.Chat.Time && chat.Chat.Time < time.AddSeconds(3)).Select(chat => chat.Chat))
                    {
                        ret.Add(chat);
                    }
                }
            }

            //取得処理
            if (chatCollectTask != null && !chatCollectTask.IsCompleted)
                return ret;//まだ前回の取得が終わってないなら帰る(普通は終わってるはず)

            try
            {
                chatCollectTask?.Wait();
            }
            catch (AggregateException e) when (e.InnerException is ServerErrorException)
            {
                throw new ChatCollectException("非公式ニコニコ実況過去ログAPIからエラーが返されました。", e);
            }
            catch (AggregateException e) when (e.InnerException is HttpRequestException)
            {
                throw new ChatCollectException("非公式ニコニコ実況過去ログAPIとの通信でエラーが発生しました。", e);
            }

            lock (chats)
            {
                //チャンネル変更もしくは取得済みコメントが0の場合履歴をクリアして取得
                if (lastJkId != jkId || chats.Count <= 0)
                {
                    chats.Clear();
                    chatCollectTask = collectChat(jkId, time.AddSeconds(1), time.AddSeconds(3600));
                    lastJkId = jkId;
                    lastGetTime = time;
                }
                //1時間後にシークした場合　普通に追加で取得
                else if (3600 < (time - lastGetTime).Seconds)
                {
                    chatCollectTask = collectChat(jkId, time.AddSeconds(1), time.AddSeconds(3600));
                    lastJkId = jkId;
                    lastGetTime = time;
                }
                //過去にシーク　履歴の一番古い情報より前にシークされてた場合はクリアして取得
                else if ((time - lastGetTime).Seconds < 0 )
                {
                    var isOlder = time < chats.OrderBy(ch => ch.Chat.Time).First().Chat.Time;
                    if (isOlder)
                    {
                        chats.Clear();
                        chatCollectTask = collectChat(jkId, time.AddSeconds(1), time.AddSeconds(3600));
                        lastJkId = jkId;
                        lastGetTime = time;
                    }
                }
            }
            return ret;
        }

        private async Task collectChat(int jkId, DateTime startTime, DateTime endTime)
        {
            string startTimeStr = new DateTimeOffset(startTime).ToUnixTimeSeconds().ToString();
            string endTimeStr = new DateTimeOffset(endTime).ToUnixTimeSeconds().ToString();
            string queryStr = await client.GetStringAsync($"https://jikkyo.tsukumijima.net/api/kakolog/jk{jkId}?starttime={startTimeStr}&endtime={endTimeStr}&format=xml").ConfigureAwait(false);
            var query = HttpUtility.ParseQueryString(queryStr);
            if (query.AllKeys.Contains("error"))
                throw new ServerErrorException();
            NiconicoUtils.NiconicoCommentXmlParser parser = new NiconicoUtils.NiconicoCommentXmlParser(false);
            parser.Push(queryStr);
            lock (chats)
            {
                while (parser.DataAvailable())
                {
                    var tag = parser.Pop();
                    var chatTag = tag as NiconicoUtils.ChatNiconicoCommentXmlTag;
                    if (chatTag != null)
                    {
                        chats.Enqueue(new NiconicoUtils.ChatAndVpos(
                                NiconicoUtils.ChatNiconicoCommentXmlTagToChat.Convert(chatTag), chatTag.Vpos
                        ));
                    }

                }
            }
        }

        public override void Dispose()
        {
            using (this.client)
            {


                client.CancelPendingRequests();
                try
                {
                    chatCollectTask?.Wait();
                }
                catch (AggregateException e) when (e.InnerException is OperationCanceledException || e.InnerException is HttpRequestException || e.InnerException is ServerErrorException) { }
            }
        }
    }
}