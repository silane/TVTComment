using ObservableUtils;
using System;
using TVTComment.Model.ChatCollectService;
using TVTComment.Model.TwitterUtils;

namespace TVTComment.Model.ChatCollectServiceEntry
{
    class TwitterLiveChatCollectServiceEntry : IChatCollectServiceEntry
    {
        public class ChatCollectServiceCreationOption : IChatCollectServiceCreationOption
        {
            public string SearchWord { get; }
            public ChatCollectServiceCreationOption(string searchWord)
            {
                SearchWord = searchWord;
            }
        }

        public ChatService.IChatService Owner { get; }
        public string Id => "TwitterLive";
        public string Name => "Twitterリアルタイム実況";
        public string Description => "指定した検索ワードでTwitter実況";
        public bool CanUseDefaultCreationOption => false;

        private readonly ObservableValue<TwitterAuthentication> Session;

        public TwitterLiveChatCollectServiceEntry(ChatService.TwitterChatService Owner, ObservableValue<TwitterAuthentication> Session)
        {
            this.Owner = Owner;
            this.Session = Session;
        }

        public IChatCollectService GetNewService(IChatCollectServiceCreationOption creationOption)
        {
            if (creationOption == null || creationOption is not ChatCollectServiceCreationOption co)
                throw new ArgumentException($"Type of {nameof(creationOption)} must be {nameof(TwitterLiveChatCollectServiceEntry)}.{nameof(ChatCollectServiceCreationOption)}", nameof(creationOption));
            var session = Session.Value;
            if (session == null)
                throw new ChatCollectServiceCreationException("Twiiterリアルタイム実況にはTwitterへのログインが必要です");
            return new TwitterLiveChatCollectService(this, co.SearchWord, session);
        }
    }
}
