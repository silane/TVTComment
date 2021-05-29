using CoreTweet;
using CoreTweet.Streaming;
using ObservableUtils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using TVTComment.Model.ChatCollectServiceEntry;
using TVTComment.Model.NiconicoUtils;
using TVTComment.Model.TwitterUtils;
using TVTComment.Model.TwitterUtils.AnnictUtils;
using static TVTComment.Model.ChatCollectServiceEntry.TwitterLiveChatCollectServiceEntry.ChatCollectServiceCreationOption;

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

        public IChatCollectServiceEntry ServiceEntry { get; }

        public bool CanPost => true;

        private readonly TwitterAuthentication Twitter;
        private Task chatCollectTask;
        private readonly ConcurrentQueue<Status> statusQueue = new ConcurrentQueue<Status>();
        private readonly CancellationTokenSource cancel = new CancellationTokenSource();
        private readonly ObservableValue<string> SearchWord = new ObservableValue<string>("");
        private readonly ObservableValue<string> EventTextWord = new ObservableValue<string>("");
        private readonly SearchWordResolver SearchWordResolver;
        private readonly ModeSelectMethod ModeSelect;
        private readonly AnnictApis Annict;

        public TwitterLiveChatCollectService(IChatCollectServiceEntry serviceEntry, string searchWord, ModeSelectMethod modeSelect, SearchWordResolver searchWordResolver, TwitterAuthentication twitter)
        {
            Twitter = twitter;
            ServiceEntry = serviceEntry;
            SearchWord.Value = searchWord;
            ModeSelect = modeSelect;
            SearchWordResolver = searchWordResolver;
            switch (modeSelect)
            {
                case ModeSelectMethod.Preset:
                    SearchWord.Where(x => x != null && !x.Equals("")).Subscribe(res => chatCollectTask = SearchStreamAsync(res, cancel.Token));
                    break;

                case ModeSelectMethod.Manual:
                    chatCollectTask = SearchStreamAsync(searchWord, cancel.Token);
                    break;
            }
            if (Twitter.AnnictToken != null && !Twitter.AnnictToken.Equals("")) { 
                Annict = new(Twitter.AnnictToken);
                EventTextWord.Where(x => x != null && !x.Equals("")).Subscribe(res => _ = annictSearch(EventTextWord.Value));
            }
        }

        public string GetInformationText()
        {
            return $"検索モード:{ModeSelect}\n検索ワード:{SearchWord.Value}";
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
                    if (cancel.IsCancellationRequested || !SearchWord.Value.Equals(searchWord))
                        break;
                    statusQueue.Enqueue(status);
                }
            }, cancel);
        }

        private async Task annictSearch(string evetnText)
        {
            var result = await Annict.GetTwitterHashtagAsync(evetnText);
            if (result != null && !result.Equals("")) SearchWord.Value += " OR #" + result;
        }

        public IEnumerable<Chat> GetChats(ChannelInfo channel,EventInfo events, DateTime time)
        {
            if (ModeSelect == ModeSelectMethod.Preset)
            {
                SearchWord.Value = SearchWordResolver.Resolve(channel.NetworkId, channel.ServiceId);
            }
            if (chatCollectTask != null)
            {
                if (chatCollectTask.IsCanceled)
                {
                    throw new ChatCollectException("Cancelしました");
                }
                if (chatCollectTask.IsFaulted)
                {
                    throw new ChatCollectException("TwitterのAPI制限に達したか問題が発生したため切断されました");
                }
            }

            var result = AnnimeTitleGetter.Convert(events.EventName);
            if (result != null && !result.Equals("")) EventTextWord.Value = result;

            var list = new List<Chat>();
            while (statusQueue.TryDequeue(out var status))
            {
                list.Add(ChatTwitterStatusToChat.Convert(status));
            }
            return list;
        }

        public async Task PostChat(BasicChatPostObject postObject)
        {
            try
            {
                var suffix = "\n" + (postObject as ChatPostObject)?.SuffixText ?? "";
                await Twitter.Token.Statuses.UpdateAsync(postObject.Text + suffix);
            }
            catch
            {
                throw new ChatPostException("ツイート投稿に失敗しました。\nTwitterAPIのApp permissionsがRead Onlyになっていないことを確認してください。");
            }
        }

        public void Dispose()
        {
            cancel.Cancel();
        }
    }
}
