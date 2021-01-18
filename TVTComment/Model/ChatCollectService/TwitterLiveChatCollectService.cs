using CoreTweet;
using CoreTweet.Streaming;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TVTComment.Model.ChatCollectServiceEntry;
using TVTComment.Model.TwitterUtils;

namespace TVTComment.Model.ChatCollectService
{
    class TwitterLiveChatCollectService : IChatCollectService
    {
        public class ChatPostObject : BasicChatPostObject
        {
            public string SuffixText { get; }
            public ChatPostObject(string text, string suffixtext) : base(text)
            {
                SuffixText = suffixtext;
            }
        }

        public string Name => "Twitterリアルタイム実況";

        public  IChatCollectServiceEntry ServiceEntry { get; }

        public bool CanPost => true;

        private readonly TwitterAuthentication Twitter;
        private readonly Task chatCollectTask;
        private readonly ConcurrentQueue<Status> statusQueue = new ConcurrentQueue<Status>();
        private readonly CancellationTokenSource cancel = new CancellationTokenSource();
        private readonly string SearchWord;


        public TwitterLiveChatCollectService(IChatCollectServiceEntry serviceEntry, string searchWord, TwitterAuthentication twitter)
        {
            ServiceEntry = serviceEntry;
            SearchWord = searchWord;
            Twitter = twitter;
            chatCollectTask = SearchStreamAsync(searchWord, cancel.Token);
        }

        public  string GetInformationText()
        {
            return "検索ワード:" + SearchWord;
        }

        private async Task SearchStreamAsync(string searchWord, CancellationToken cancel)
        {
            await Task.Run(() =>
            {
                foreach (var status in Twitter.Token.Streaming.Filter(track: searchWord)
                            .OfType<StatusMessage>()
                            .Where(x => !x.Status.Text.StartsWith("RT"))
                            .Select(x => x.Status))
                {
                    if (cancel.IsCancellationRequested) break ;
                    statusQueue.Enqueue(status);
                }
            }, cancel);
        }

        public IEnumerable<Chat> GetChats(ChannelInfo channel, DateTime time)
        {
            if (chatCollectTask.IsCanceled)
            {
                throw new ChatCollectException("Cancelしました");
            }
            if (chatCollectTask.IsFaulted)
            {
                throw new ChatCollectException("ストリームが切断されました");
            }
            var list = new List<Chat>();
            while (statusQueue.TryDequeue(out var status))
            {
                list.Add(ChatTwitterStatusToChat.Convert(status));
            }
            return list;
        }

        public Task PostChat(BasicChatPostObject postObject)
        {
            var suffix = "\n"+(postObject as ChatPostObject)?.SuffixText ?? "";
            return Twitter.Token.Statuses.UpdateAsync(postObject.Text + suffix);
        }

        public void Dispose()
        {
            cancel.Cancel();
        }
    }
}
