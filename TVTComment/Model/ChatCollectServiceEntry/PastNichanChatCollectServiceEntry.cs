using System;
using System.Collections.Generic;
using System.Text;

namespace TVTComment.Model.ChatCollectServiceEntry
{
    class PastNichanChatCollectServiceEntry : IChatCollectServiceEntry
    {
        public ChatService.IChatService Owner { get; }

        public string Id => "2chPast";
        public string Name => "2ch過去ログ";
        public string Description => "2chの過去ログを自動で表示";

        public bool CanUseDefaultCreationOption => true;

        public PastNichanChatCollectServiceEntry(ChatService.NichanChatService chatService, NichanUtils.ThreadResolver threadResolver)
        {
            this.Owner = chatService;
            this.threadResolver = threadResolver;
        }

        public ChatCollectService.IChatCollectService GetNewService(IChatCollectServiceCreationOption creationOption)
        {
            var boardSelector = new NichanUtils.AutoNichanBoardSelector(this.threadResolver);
            return new ChatCollectService.PastNichanChatCollectService(this, boardSelector);
        }

        private readonly NichanUtils.ThreadResolver threadResolver;
    }
}
