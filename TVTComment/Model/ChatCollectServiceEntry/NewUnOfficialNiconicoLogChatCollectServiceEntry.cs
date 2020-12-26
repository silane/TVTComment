using System;
using TVTComment.Model.ChatService;

namespace TVTComment.Model.ChatCollectServiceEntry
{
    class NewUnOfficialNiconicoLogChatCollectServiceEntry : IChatCollectServiceEntry
    {
        public class ChatCollectServiceCreationOption:IChatCollectServiceCreationOption
        {
        }

        public ChatService.IChatService Owner { get; }
        public string Id => "UnOfficialNiconicoLog";
        public string Name => "非公式ニコニコ実況過去ログ";
        public string Description => "非公式ニコニコ実況過去ログを自動で表示";
        public bool CanUseDefaultCreationOption => true;

        private NiconicoChatServiceSettings settings;
        private NiconicoUtils.JkIdResolver jkIdResolver;

        public NewUnOfficialNiconicoLogChatCollectServiceEntry(ChatService.NiconicoChatService owner,NiconicoUtils.JkIdResolver jkIdResolver, NiconicoChatServiceSettings settings)
        {
            this.Owner = owner;
            this.jkIdResolver = jkIdResolver;
            this.settings = settings;
        }
        public ChatCollectService.IChatCollectService GetNewService(IChatCollectServiceCreationOption creationOption)
        {
            if(creationOption!=null && !(creationOption is ChatCollectServiceCreationOption))
                throw new ArgumentException($"Type of {nameof(creationOption)} must be {nameof(NewUnOfficialNiconicoLogChatCollectServiceEntry)}.{nameof(ChatCollectServiceCreationOption)}", nameof(creationOption));
            
            return new ChatCollectService.NewUnOfficialNiconicoLogChatCollectService(this, jkIdResolver,this.settings);
        }
    }
}
