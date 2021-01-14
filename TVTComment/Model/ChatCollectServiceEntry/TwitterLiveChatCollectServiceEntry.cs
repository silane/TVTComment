using ObservableUtils;
using System;
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
                this.SearchWord = searchWord;
            }
        }

        public ChatService.IChatService Owner { get; }
        public string Id => "TwitterLive";
        public string Name => "Twitterリアルタイム実況";
        public string Description => "指定した検索ワードでTwitter実況";
        public bool CanUseDefaultCreationOption => false;

        private ObservableValue<TwitterAuthentication> Session;

        public TwitterLiveChatCollectServiceEntry(ChatService.TwitterChatService Owner, ObservableValue<TwitterAuthentication> Session)
        {
            this.Owner = Owner;
            this.Session = Session;
        }

        public ChatCollectService.IChatCollectService GetNewService(IChatCollectServiceCreationOption creationOption)
        {
            return null;
        }
    }
}
