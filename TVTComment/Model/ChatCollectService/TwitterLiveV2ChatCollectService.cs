using CoreTweet;
using CoreTweet.Streaming;
using CoreTweet.V2;
using ObservableUtils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using TVTComment.Model.ChatCollectServiceEntry;
using TVTComment.Model.NiconicoUtils;
using TVTComment.Model.TwitterUtils;
using static TVTComment.Model.ChatCollectServiceEntry.TwitterLiveChatCollectServiceEntry.ChatCollectServiceCreationOption;

namespace TVTComment.Model.ChatCollectService
{
    class TwitterLiveV2ChatCollectService : IChatCollectService
    {
        public class ChatPostObject : BasicChatPostObject
        {
            public string SuffixText { get; }
            public ChatPostObject(string text, string suffixtext) : base(text)
            {
                SuffixText = suffixtext;
            }
        }

        public string Name => "Twitterリアルタイム実況 v2 API版";

        public IChatCollectServiceEntry ServiceEntry { get; }

        public bool CanPost => true;

        private readonly TwitterAuthentication Twitter;
        private Task chatCollectTask;
        private string oldSearchWord ="";
        private readonly ConcurrentQueue<FilterStreamResponse> statusQueue = new ();
        private readonly CancellationTokenSource cancel = new ();
        private readonly ObservableValue<string> SearchWord = new ("");
        private readonly SearchWordResolver SearchWordResolver;
        private readonly ModeSelectMethod ModeSelect;


        public TwitterLiveV2ChatCollectService(IChatCollectServiceEntry serviceEntry, string searchWord, ModeSelectMethod modeSelect, SearchWordResolver searchWordResolver, TwitterAuthentication twitter)
        {
            ServiceEntry = serviceEntry;
            Twitter = twitter;
            ModeSelect = modeSelect;
            SearchWordResolver = searchWordResolver;
            if (Twitter.OAuth2Token == null)
                throw new ChatCollectServiceCreationException("Twitter API v2の利用に失敗");
            
            SearchWord.Where(x => x != null && !x.Equals("")).Subscribe(res => {
                var filter = new FilterRule();
                filter.Value = res;
                Twitter.OAuth2Token.V2.FilteredStreamApi.CreateRules(add: new[] { filter });
                Twitter.OAuth2Token.V2.FilteredStreamApi.DeleteRules(values: new[] { oldSearchWord });
                oldSearchWord = res;
            });
            SearchWord.Value = searchWord;
            chatCollectTask = SearchStreamAsync(cancel.Token);
        }

        public string GetInformationText()
        {
            return $"検索モード:{ModeSelect}\n検索ワード:{SearchWord.Value}";
        }

        private async Task SearchStreamAsync(CancellationToken cancel)
        {
            await Task.Run(() =>
            {
                foreach (var status in Twitter.OAuth2Token.V2.FilteredStreamApi.Filter(expansions: TweetExpansions.AuthorId, tweet_fields: TweetFields.CreatedAt)
                        .StreamAsEnumerable().OfType<FilterStreamResponse>())
                {
                    if (cancel.IsCancellationRequested)
                        break;
                    statusQueue.Enqueue(status);
                }
            }, cancel);
        }

        public IEnumerable<Chat> GetChats(ChannelInfo channel, DateTime time)
        {
            if (ModeSelect == ModeSelectMethod.Auto)
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
            Twitter.OAuth2Token.V2.FilteredStreamApi.DeleteRules(values: new[] { SearchWord.Value });
            cancel.Cancel();
        }
    }
}
