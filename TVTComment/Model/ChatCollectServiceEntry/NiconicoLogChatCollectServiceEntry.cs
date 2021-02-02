using ObservableUtils;
using System;

namespace TVTComment.Model.ChatCollectServiceEntry
{
    class NiconicoLogChatCollectServiceEntry : IChatCollectServiceEntry
    {
        public class ChatCollectServiceCreationOption : IChatCollectServiceCreationOption
        {
        }

        public ChatService.IChatService Owner { get; }
        public string Id => "NiconicoLog";
        public string Name => "ニコニコ実況過去ログ";
        public string Description => "ニコニコ実況過去ログを自動で表示";
        public bool CanUseDefaultCreationOption => true;

        private readonly NiconicoUtils.JkIdResolver jkIdResolver;
        private readonly ObservableValue<NiconicoUtils.NiconicoLoginSession> session;

        public NiconicoLogChatCollectServiceEntry(ChatService.NiconicoChatService owner, NiconicoUtils.JkIdResolver jkIdResolver, ObservableValue<NiconicoUtils.NiconicoLoginSession> loginSession)
        {
            Owner = owner;
            this.jkIdResolver = jkIdResolver;
            session = loginSession;
        }
        public ChatCollectService.IChatCollectService GetNewService(IChatCollectServiceCreationOption creationOption)
        {
            if (creationOption != null && !(creationOption is ChatCollectServiceCreationOption))
                throw new ArgumentException($"Type of {nameof(creationOption)} must be {nameof(NiconicoLogChatCollectServiceEntry)}.{nameof(ChatCollectServiceCreationOption)}", nameof(creationOption));

            if (session.Value == null || !session.Value.IsLoggedin)
                throw new ChatCollectServiceCreationException("ニコニコ動画にログインしていないためニコニコ実況過去ログは使えません");

            return new ChatCollectService.NiconicoLogChatCollectService(this, jkIdResolver, session.Value);
        }
    }
}
