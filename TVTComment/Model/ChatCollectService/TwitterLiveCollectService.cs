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
    class TwitterLiveCollectService : IChatCollectService
    {

        public  string Name => "Twitterリアルタイム実況";

        public  IChatCollectServiceEntry ServiceEntry { get; }

        public bool CanPost => true;

        private TwitterAuthentication Twitter;
        private readonly Task chatCollectTask;
        private readonly ConcurrentQueue<Status> statusQueue = new ConcurrentQueue<Status>();
        private readonly CancellationTokenSource cancel = new CancellationTokenSource();


        public TwitterLiveCollectService(IChatCollectServiceEntry serviceEntry, string searchWord, TwitterAuthentication twitter)
        {
            ServiceEntry = serviceEntry;
            Twitter = twitter;
            chatCollectTask = SearchStreamAsync(searchWord, cancel.Token);
        }

        public  string GetInformationText()
        {
            return "";
        }

        private async Task SearchStreamAsync(string searchWord, CancellationToken cancel)
        {
            /*
            var stream = Twitter.Token.Streaming.FilterAsObservable(track: searchWord).Publish();
            stream.OfType<StatusMessage>()
                .Subscribe((x) => {
                    statusQueue.Enqueue(x.Status);
                    Debug.WriteLine(x.Status.Text);});
            disposableStream  = stream.Connect();*/

            await Task.Run(() =>
            {
                foreach (var status in Twitter.Token.Streaming.Filter(track: searchWord)
                            .OfType<StatusMessage>()
                            .Where(x => !x.Status.Text.StartsWith("RT"))
                            .Select(x => x.Status))
                {
                    if (cancel.IsCancellationRequested) break ;
                    statusQueue.Enqueue(status);
                    Debug.WriteLine(status.Text);
                }
            });

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
            Debug.WriteLine(statusQueue.Count);
            var list = new List<Chat>();
            while (statusQueue.TryDequeue(out var status))
            {
                list.Add(ChatTwitterStatusToChat.Convert(status));
            }
            return list;
        }

        public Task PostChat(BasicChatPostObject postObject)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            cancel.Cancel();
        }
    }
}
