using ObservableUtils;
using System;

namespace TVTComment.Model.ChatCollectServiceEntry
{
    class NiconicoLiveChatCollectServiceEntry : IChatCollectServiceEntry
    {
        public class ChatCollectServiceCreationOption : IChatCollectServiceCreationOption
        {
            public string LiveId { get; }
            public ChatCollectServiceCreationOption(string liveId)
            {
                this.LiveId = liveId;
            }
        }

        public ChatService.IChatService Owner { get; }
        public string Id => "NiconicoLive";
        public string Name => "ニコニコ生放送";
        public string Description => "指定したニコニコ生放送を表示";
        public bool CanUseDefaultCreationOption => false;

        private ObservableValue<NiconicoUtils.NiconicoLoginSession> session;

        public NiconicoLiveChatCollectServiceEntry(ChatService.NiconicoChatService owner, ObservableValue<NiconicoUtils.NiconicoLoginSession> session)
        {
            this.Owner = owner;
            this.session = session;
        }

        public ChatCollectService.IChatCollectService GetNewService(IChatCollectServiceCreationOption creationOption)
        {
            if (creationOption == null || !(creationOption is ChatCollectServiceCreationOption co))
                throw new ArgumentException($"Type of {nameof(creationOption)} must be {nameof(NiconicoLiveChatCollectServiceEntry)}.{nameof(ChatCollectServiceCreationOption)}", nameof(creationOption));
            var session = this.session.Value;
            if (session == null)
                throw new ChatCollectServiceCreationException("ニコニコ生放送にはニコニコへのログインが必要です");
            return new ChatCollectService.NiconicoLiveChatCollectService(this, co.LiveId, this.session.Value);
        }
    }
}
