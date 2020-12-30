namespace TVTComment.Model.ChatCollectServiceEntry
{
    class TsukumijimaJikkyoApiChatCollectServiceEntry : IChatCollectServiceEntry
    {
        public ChatService.IChatService Owner { get; }
        public string Id => "TsukumijimaJikkyoApi";
        public string Name => "非公式ニコニコ実況過去ログ";
        public string Description => "tsukumijimaさんが提供しているニコニコ実況の過去ログAPIからコメントを表示";
        public bool CanUseDefaultCreationOption => true;
        
        public TsukumijimaJikkyoApiChatCollectServiceEntry(
            ChatService.IChatService chatService,
            NiconicoUtils.JkIdResolver jkIdResolver
        )
        {
            this.Owner = chatService;
            this.jkIdResolver = jkIdResolver;
        }

        public ChatCollectService.IChatCollectService GetNewService(IChatCollectServiceCreationOption creationOption)
        {
            return new ChatCollectService.TsukumijimaJikkyoApiChatCollectService(this, this.jkIdResolver);
        }

        private readonly NiconicoUtils.JkIdResolver jkIdResolver;
    }
}
