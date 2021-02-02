using ObservableUtils;
using System;

namespace TVTComment.Model.ChatCollectServiceEntry
{
    class NiconicoChatCollectServiceEntry : IChatCollectServiceEntry
    {
        public class ChatCollectServiceCreationOption : IChatCollectServiceCreationOption
        {
        }

        public ChatService.IChatService Owner { get; }
        public string Id => "Niconico";
        public string Name => "ニコニコ実況";
        public string Description => "現在のニコニコ実況を表示";
        public bool CanUseDefaultCreationOption => true;

        private readonly NiconicoUtils.JkIdResolver jkIdResolver;
        private readonly ObservableValue<NiconicoUtils.NiconicoLoginSession> session;

        public NiconicoChatCollectServiceEntry(ChatService.NiconicoChatService owner, NiconicoUtils.JkIdResolver jkIdResolver, ObservableValue<NiconicoUtils.NiconicoLoginSession> session)
        {
            Owner = owner;
            this.jkIdResolver = jkIdResolver;
            this.session = session;
        }

        public ChatCollectService.IChatCollectService GetNewService(IChatCollectServiceCreationOption creationOption)
        {
            if (creationOption != null && !(creationOption is ChatCollectServiceCreationOption))
                throw new ArgumentException($"Type of {nameof(creationOption)} must be {nameof(NiconicoChatCollectServiceEntry)}.{nameof(ChatCollectServiceCreationOption)}", nameof(creationOption));
            return new ChatCollectService.NiconicoChatCollectService(this, jkIdResolver, session.Value);
        }
    }
}
