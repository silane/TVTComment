using ObservableUtils;
using System;

namespace TVTComment.Model.ChatCollectServiceEntry
{
    class NewNiconicoJikkyouChatCollectServiceEntry : IChatCollectServiceEntry
    {
        public class ChatCollectServiceCreationOption : IChatCollectServiceCreationOption
        {
        }

        public ChatService.IChatService Owner { get; }
        public string Id => "NewNiconicoJikkyou";
        public string Name => "新ニコニコ実況";
        public string Description => "現在のニコニコ実況を表示";
        public bool CanUseDefaultCreationOption => true;

        private readonly NiconicoUtils.LiveIdResolver liveIdResolver;
        private readonly ObservableValue<NiconicoUtils.NiconicoLoginSession> session;

        public NewNiconicoJikkyouChatCollectServiceEntry(
            ChatService.NiconicoChatService owner, NiconicoUtils.LiveIdResolver liveIdResolver,
            ObservableValue<NiconicoUtils.NiconicoLoginSession> session
        )
        {
            Owner = owner;
            this.liveIdResolver = liveIdResolver;
            this.session = session;
        }

        public ChatCollectService.IChatCollectService GetNewService(IChatCollectServiceCreationOption creationOption)
        {
            if (creationOption != null && !(creationOption is ChatCollectServiceCreationOption))
                throw new ArgumentException($"Type of {nameof(creationOption)} must be {nameof(NewNiconicoJikkyouChatCollectServiceEntry)}.{nameof(ChatCollectServiceCreationOption)}", nameof(creationOption));
            var session = this.session.Value;
            if (session == null)
                throw new ChatCollectServiceCreationException("ニコニコ生放送にはニコニコへのログインが必要です");
            return new ChatCollectService.NewNiconicoJikkyouChatCollectService(this, liveIdResolver, session);
        }
    }
}
